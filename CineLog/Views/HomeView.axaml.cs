using Dapper;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using CineLog.ViewModels;

namespace CineLog.Views
{
    public partial class HomeView : UserControl
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
        private static readonly string dbPath = "example.db";
        private static readonly string connectionString = $"Data Source={dbPath};Version=3;";
        private static readonly string[] sourceArray = ["titles", "lists"];

        public HomeView()
        {
            InitializeComponent();
            CreateListsTable();
            _ = LoadCollection();
            _ = LoadLists();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task LoadLists()
        {
            var lists = GetListsFromDatabase(); // Get all list names from the database.

            foreach (var listName in lists)
            {
                // await LoadCollection(listName);
            }
        }

        private List<string> GetListsFromDatabase()
        {
            List<string> lists = new();
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            using var command = new SQLiteCommand("SELECT name FROM lists;", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(reader.GetString(0));
                lists.Add(reader.GetString(0));
            }

            return lists;
        }

        private async Task LoadCollection(string listName = "titles")
        {
            List<(string Title, string PosterUrl)> titles = GetMoviesFromList(listName);
            StackPanel? panel = this.FindControl<StackPanel>(listName);

            foreach (var (title, posterUrl) in titles)
            {
                Border movieBox = new()
                {
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    Width = 150,
                    Height = 250
                };

                StackPanel contentPanel = new()
                {
                    Orientation = Orientation.Vertical
                };

                Image movieImage = new()
                {
                    Stretch = Stretch.UniformToFill,
                    Width = 150,
                    Height = 200
                };

                await LoadImageFromUrl(movieImage, posterUrl);

                Border imageBorder = new()
                {
                    CornerRadius = new CornerRadius(10),
                    ClipToBounds = true,
                    Child = movieImage
                };

                TextBlock movieTitle = new()
                {
                    Text = title,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 14,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                contentPanel.Children.Add(imageBorder);
                contentPanel.Children.Add(movieTitle);
                movieBox.Child = contentPanel;
                
                panel?.Children.Add(movieBox);
            }
        }

        private static async Task LoadImageFromUrl(Image imageControl, string imageUrl)
        {
            try
            {
                using HttpClient client = new();
                byte[] imageData = await client.GetByteArrayAsync(imageUrl);

                using MemoryStream stream = new(imageData);
                imageControl.Source = new Bitmap(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image: {ex.Message}");
            }
        }

        private List<(string Title, string PosterUrl)> GetMoviesFromList(string table_name = "titles")
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            Console.WriteLine("listname: " + table_name);

            // Validate table name to prevent SQL injection (optional but recommended)
            if (!sourceArray.Contains(table_name))
            {
                Console.WriteLine(" i thrwo");
                throw new ArgumentException("Invalid table name", nameof(table_name));
            }

            Console.WriteLine("Table name validation passed");

            string query = $"SELECT title_name, poster_url FROM {table_name}";
            
            return connection.Query<(string, string)>(query).AsList();
        }

        private static void CreateListsTable() {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS lists (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE
                );
            ");
        }

        private void AddListToTable(object sender, RoutedEventArgs e)
        {
            string newListName = $"CustomList#{GetNextListId()}";

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            connection.Execute("INSERT INTO lists (name) VALUES (@name)", new { name = newListName });

            LoadOrAddList(newListName);
        }

        private static int GetNextListId()
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            return connection.ExecuteScalar<int>("SELECT COALESCE(MAX(id), 0) + 1 FROM lists");
        }

        private async void LoadOrAddList(string listName)
        {
            // Check if the list already exists in UI
            StackPanel? existingListPanel = this.FindControl<StackPanel>(listName);
            if (existingListPanel == null)
            {
                // existingListPanel = CreateListUI(listName);
                CreateListUI(listName);
            }

            Console.WriteLine("listname: " + listName);

            await LoadCollection(listName);
        }

        private StackPanel CreateListUI(string listName)
        {
            DockPanel dockPanel = new()
            {
                LastChildFill = true
            };

            TextBlock listTitle = new()
            {
                Text = listName,
                Foreground = Brushes.White,
                FontSize = 16
            };

            Button seeAllButton = new()
            {
                Content = "See all",
                FontSize = 16,
                Tag = listName,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.White
            };
            seeAllButton.Click += ViewChanger;

            DockPanel.SetDock(seeAllButton, Dock.Right);
            dockPanel.Children.Add(listTitle);
            dockPanel.Children.Add(seeAllButton);

            StackPanel listPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Name = listName
            };

            Border listContainer = new()
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = listPanel
                }
            };

            return listPanel;
        }

        #region ViewModifier
            private void ViewChanger(object? sender, RoutedEventArgs e)
            {
                if (sender is Button button && button.Tag is string viewName)
                {
                    ViewModel?.HandleButtonClick(viewName);
                }
            }
        #endregion
    }
}