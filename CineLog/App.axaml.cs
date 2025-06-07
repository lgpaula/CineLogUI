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

namespace CineLog;

public class App : Application
{
    private Process? _pythonServerProcess;
    private CancellationTokenSource? _workerTokenSource;

    public override void Initialize()
    {
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
        Console.WriteLine("Current Directory: " + GetWorkingDir());
        if (IsServerRunning()) return;
        _ = WaitForFlaskReady();

        _pythonServerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = GetDefaultPythonExecutable(),
                Arguments = "scraper_api.py",
                WorkingDirectory = GetWorkingDir(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _pythonServerProcess.Start();
        _pythonServerProcess.OutputDataReceived += (_, e) => Console.WriteLine("[Python STDOUT] " + e.Data);
        _pythonServerProcess.ErrorDataReceived += (_, e) => Console.WriteLine("[Python STDERR] " + e.Data);
        _pythonServerProcess.BeginOutputReadLine();
        _pythonServerProcess.BeginErrorReadLine();
    }
    
    private static string GetDefaultPythonExecutable()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python" : "python3";
    }
    
    private static string GetWorkingDir()
    {
        var baseDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."));

        string[] relativePaths =
        [
            Path.Combine("backend", "data_scraper"),
            Path.Combine("pyScraper", "data_scraper")
        ];

        foreach (var relPath in relativePaths)
        {
            var fullPath = Path.Combine(baseDir, relPath);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new DirectoryNotFoundException("No valid working directory found relative to current directory.");
    }
    
    // dev Current Directory: /home/legion/CLionProjects/CineLogUI/CineLog
    // deploy Current Directory: /home/legion/CLionProjects/CineLogAppRelease/frontend/linux

    private static async Task WaitForFlaskReady()
    {
        using var client = new HttpClient();
        for (var i = 0; i < 20; i++) // try for 10 seconds
        {
            try
            {
                var res = await client.GetAsync("http://127.0.0.1:5000/health");
                if (res.IsSuccessStatusCode)
                {
                    Console.WriteLine("Flask is ready.");
                    return;
                }
            }
            catch
            {
                // ignored
            }

            await Task.Delay(500);
        }

        Console.WriteLine("Flask did not become ready in time.");
        EventAggregator.Instance.Publish(new NotificationEvent { Message = "X The server failed to start. Scraping and updating will not work" });
    }

    private static bool IsServerRunning()
    {
        try
        {
            using var client = new HttpClient();
            var response = client.GetAsync("http://127.0.0.1:5000/health").Result;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void OnExit()
    {
        _pythonServerProcess?.Kill();
        _pythonServerProcess?.Dispose();
    }

    public void RestartWorkerThreads()
    {
        _workerTokenSource?.Cancel();
        _workerTokenSource?.Dispose();

        _workerTokenSource = new CancellationTokenSource();
        _ = StartWorkerThreadsAsync(_workerTokenSource.Token);
    }

    private static async Task StartWorkerThreadsAsync(CancellationToken token)
    {
        const int maxConcurrentTasks = 2;

        var infoGatherer = Task.Run(async () =>
        {
            await WaitForFlaskReady();

            var sqlQuery = new DatabaseHandler.SqlQuerier();
            var dbTitles = DatabaseHandler.GetMovies(sqlQuery);

            using var throttler = new SemaphoreSlim(maxConcurrentTasks);
            var tasks = dbTitles.Select(async title =>
            {
                await throttler.WaitAsync(token);
                try
                {
                    token.ThrowIfCancellationRequested();
                    await DatabaseHandler.UpdateTitleInfo(title.Id);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);
        }, token);

        await infoGatherer;
        var episodeFetcher = Task.Run(async () =>
        {
            await WaitForFlaskReady();

            var sqlQuery = new DatabaseHandler.SqlQuerier();
            var dbTitles = DatabaseHandler.GetMovies(sqlQuery);

            using var throttler = new SemaphoreSlim(maxConcurrentTasks);
            var tasks = dbTitles.Select(async title =>
            {
                await throttler.WaitAsync(token);
                try
                {
                    token.ThrowIfCancellationRequested();
                    await DatabaseHandler.FetchEpisodes(title.Id);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);
        }, token);

        await episodeFetcher;
    }
}