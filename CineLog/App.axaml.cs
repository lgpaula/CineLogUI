using System.Diagnostics;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System.Threading.Tasks;
using System;
using System.IO;
using CineLog.Views.Helper;
using System.Threading;
using System.Linq;
using System.Runtime.InteropServices;
using CineLog.Views;
using Serilog;

namespace CineLog;

public class App : Application
{
    private Process? _pythonServerProcess;
    private CancellationTokenSource? _workerTokenSource;
    public static ILogger? Logger;
    private static readonly string BaseDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."));

    public override void Initialize()
    {
        Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/frontend.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Logger.Information("App initializing...");
        AvaloniaXamlLoader.Load(this);
        StartPythonServer();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            Styles.Add(new Style(x => x.OfType<Button>())
            {
                Setters =
                {
                    new Setter(InputElement.CursorProperty, new Cursor(StandardCursorType.Hand))
                }
            });

            desktop.Exit += (_, _) => OnExit();
        }

        base.OnFrameworkInitializationCompleted();

        _workerTokenSource = new CancellationTokenSource();
        _ = StartWorkerThreadsAsync(_workerTokenSource.Token);
    }

    private void StartPythonServer()
    {
        var workingDir = GetWorkingDir();
        Console.WriteLine("Current Directory: " + workingDir);
        Logger?.Information("Attempting to start Python server from directory: {Dir}", workingDir);

        if (IsServerRunning())
        {
            Logger?.Information("Python server is already running.");
            return;
        }

        _ = WaitForFlaskReady();

        try
        {
            _pythonServerProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetDefaultPythonExecutable(),
                    Arguments = "scraper_api.py",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _pythonServerProcess.Start();
            Logger?.Information("Python server process started.");
            _pythonServerProcess.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) Logger?.Information("[Python STDOUT] {Output}", e.Data);
            };
            _pythonServerProcess.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) Logger?.Error("[Python STDERR] {Error}", e.Data);
            };
            _pythonServerProcess.BeginOutputReadLine();
            _pythonServerProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "Failed to start the Python server.");
        }
    }

    private static string GetDefaultPythonExecutable()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python" : "python3";
    }

    private static string GetWorkingDir()
    {
        string[] relativePaths =
        [
            Path.Combine("backend", "data_scraper"),
            Path.Combine("pyScraper", "data_scraper")
        ];

        foreach (var relPath in relativePaths)
        {
            var fullPath = Path.Combine(BaseDir, relPath);
            if (!Directory.Exists(fullPath)) continue;

            Logger?.Information("Found working directory: {Path}", fullPath);
            return fullPath;
        }

        Logger?.Error("No valid working directory found.");
        throw new DirectoryNotFoundException("No valid working directory found relative to current directory.");
    }

    private static async Task WaitForFlaskReady()
    {
        using var client = new HttpClient();
        for (var i = 0; i < 20; i++)
        {
            try
            {
                var res = await client.GetAsync("http://127.0.0.1:5000/health");
                if (res.IsSuccessStatusCode)
                {
                    Logger?.Information("Flask server is ready.");
                    Console.WriteLine("Flask is ready.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger?.Warning("Attempt {Attempt}: Flask not ready yet. Exception: {Message}", i + 1, ex.Message);
            }

            await Task.Delay(500);
        }

        Logger?.Error("Flask server did not become ready in time.");
        Console.WriteLine("Flask did not become ready in time.");
        EventAggregator.Instance.Publish(new NotificationEvent { Message = "\u274c The server failed to start. Scraping and updating will not work" });
    }

    private static bool IsServerRunning()
    {
        try
        {
            using var client = new HttpClient();
            var response = client.GetAsync("http://127.0.0.1:5000/health").Result;
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger?.Warning("Server check failed: {Message}", ex.Message);
            return false;
        }
    }

    private void OnExit()
    {
        Logger?.Information("Application exiting. Terminating Python server...");
        try
        {
            _pythonServerProcess?.Kill(true);
            _pythonServerProcess?.Dispose();
            Logger?.Information("Python server terminated successfully.");
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "Failed to terminate Python server.");
        }

        _workerTokenSource?.Cancel();
        _workerTokenSource?.Dispose();
    }

    public void RestartWorkerThreads()
    {
        Logger?.Information("Restarting worker threads...");
        _workerTokenSource?.Cancel();
        _workerTokenSource?.Dispose();

        _workerTokenSource = new CancellationTokenSource();
        _ = StartWorkerThreadsAsync(_workerTokenSource.Token);
    }

    private static async Task StartWorkerThreadsAsync(CancellationToken token)
    {
        const int maxConcurrentTasks = 2;
        Logger?.Information("Starting worker threads...");

        var infoGatherer = Task.Run(async () =>
        {
            await WaitForFlaskReady();

            Logger?.Information("Starting info gatherer task.");
            var sqlQuery = new DatabaseHandler.SqlQuerier();
            var dbTitles = DatabaseHandler.GetMovies(sqlQuery);

            using var throttler = new SemaphoreSlim(maxConcurrentTasks);
            var tasks = dbTitles.Select(async title =>
            {
                await throttler.WaitAsync(token);
                try
                {
                    token.ThrowIfCancellationRequested();
                    Logger?.Debug("Updating title info for ID: {Id}", title.Id);
                    await DatabaseHandler.UpdateTitleInfo(title.Id);
                }
                catch (OperationCanceledException)
                {
                    Logger?.Warning("Info gatherer canceled.");
                }
                catch (Exception ex)
                {
                    Logger?.Error(ex, "Error updating title info for ID: {Id}", title.Id);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);
            Logger?.Information("Info gatherer task completed.");
        }, token);

        await infoGatherer;

        var episodeFetcher = Task.Run(async () =>
        {
            await WaitForFlaskReady();

            Logger?.Information("Starting episode fetcher task.");
            var sqlQuery = new DatabaseHandler.SqlQuerier();
            var dbTitles = DatabaseHandler.GetMovies(sqlQuery);

            using var throttler = new SemaphoreSlim(maxConcurrentTasks);
            var tasks = dbTitles.Select(async title =>
            {
                await throttler.WaitAsync(token);
                try
                {
                    token.ThrowIfCancellationRequested();
                    Logger?.Debug("Fetching episodes for ID: {Id}", title.Id);
                    await DatabaseHandler.FetchEpisodes(title.Id);
                }
                catch (OperationCanceledException)
                {
                    Logger?.Warning("Episode fetcher canceled.");
                }
                catch (Exception ex)
                {
                    Logger?.Error(ex, "Error fetching episodes for ID: {Id}", title.Id);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);
            Logger?.Information("Episode fetcher task completed.");
        }, token);

        await episodeFetcher;
        Logger?.Information("All worker threads completed.");
    }
}