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
            _ = WaitForFlaskReady();

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
            Thread infoGatherer = new(() =>
            {
                WaitForFlaskReady().GetAwaiter().GetResult();

                var sqlQuery = new DatabaseHandler.SQLQuerier();
                var custom_lists = DatabaseHandler.GetListsFromDatabase();
                foreach (var (list_uuid, _) in custom_lists)
                {
                    sqlQuery.List_uuid = list_uuid;
                    var titles = DatabaseHandler.GetMovies(sqlQuery);
                    foreach (var title in titles)
                    {
                        DatabaseHandler.UpdateTitleInfo(title.Id).GetAwaiter().GetResult();
                    }
                }
                sqlQuery.List_uuid = null;
                var dbTitles = DatabaseHandler.GetMovies(sqlQuery);
                foreach (var title in dbTitles)
                {
                    DatabaseHandler.UpdateTitleInfo(title.Id).GetAwaiter().GetResult();
                }
            })
            {
                IsBackground = true,
            };
            infoGatherer.Start();
        }
    }
}