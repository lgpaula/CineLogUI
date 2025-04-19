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
using CineLog.Views.Helper;
using System.Threading;

namespace CineLog
{
    public partial class App : Application
    {
        private Process? _pythonServerProcess;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            StartPythonServer();
            StartWorkerThreads();
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

                desktop.Exit += (sender, args) => OnExit(sender!, args);
            }


            base.OnFrameworkInitializationCompleted();
        }

        private void StartPythonServer()
        {
            if (IsServerRunning()) return;

            _pythonServerProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python3", // Change to "python" if on Windows
                    Arguments = "scraper_api.py",
                    WorkingDirectory = "/home/legion/CLionProjects/pyScraper/scraper", //Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            _pythonServerProcess.Start();
            _ = WaitForFlaskReady();
        }

        private static async Task WaitForFlaskReady()
        {
            using var client = new HttpClient();
            for (int i = 0; i < 20; i++) // try for 10 seconds
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
                catch { }

                await Task.Delay(500);
            }

            Console.WriteLine("Flask did not become ready in time.");
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

        private void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            _pythonServerProcess?.Kill();
            _pythonServerProcess?.Dispose();
        }

        private static void StartWorkerThreads() 
        {

            Console.WriteLine("thread cal");

            Thread infoGatherer = new(async () =>
            {
                Console.WriteLine("inside");
                var lists = DatabaseHandler.GetListsFromDatabase();
                foreach (var (list_uuid, _) in lists)
                {
                    var titles = DatabaseHandler.GetMovies(list_uuid);
                    foreach (var title in titles)
                    {
                        await DatabaseHandler.UpdateTitleInfo(title.Id);
                    }
                }
                var dbTitles = DatabaseHandler.GetMovies();
                foreach (var title in dbTitles)
                {
                    await DatabaseHandler.UpdateTitleInfo(title.Id);
                }
            })
            {
                IsBackground = true,
            };
            infoGatherer.Start();
        }
    }
}