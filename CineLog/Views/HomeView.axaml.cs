using Dapper;
using System;
using System.IO;
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

        public HomeView()
        {
            InitializeComponent();
            CreateListsTable();
            _ = LoadCollection("CollectionContainer", GetMoviesFromCollection());
            _ = LoadLists();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task LoadLists()
        {
            var lists = GetListsFromDatabase();

            foreach (var listName in lists)
            {
                CreateListUI(listName); // ✅ Ensure the list shows even if empty
                var movies = GetMoviesFromList(listName);
                await LoadCollection(listName, movies);
            }
        }

        private static List<string> GetListsFromDatabase()
        {
            List<string> lists = [];
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            using var command = new SQLiteCommand("SELECT name FROM lists_table;", connection);
            using var reader = command.ExecuteReader();
            Console.WriteLine("Current lists: ");
            while (reader.Read())
            {
                Console.WriteLine(reader.GetString(0));
                lists.Add(reader.GetString(0));
            }

            return lists;
        }

        private async Task LoadCollection(string panelName, List<(string Title, string PosterUrl)> titles)
        {
            StackPanel? panel = this.FindControl<StackPanel>(panelName);

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

        private List<(string Title, string PosterUrl)> GetMoviesFromCollection()
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "SELECT title_name, poster_url FROM titles_table";
            return connection.Query<(string, string)>(query).AsList();
        }

        private static List<(string Title, string PosterUrl)> GetMoviesFromList(string listName)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT t.title_name, t.poster_url
                FROM titles_table t
                JOIN list_movies_table lm ON t.id = lm.movie_id
                JOIN lists_table l ON lm.list_id = l.id
                WHERE l.name = @ListName";

            return connection.Query<(string, string)>(query, new { ListName = listName }).AsList();
        }

        private void AddMovieToList(string listName, int movieId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            // Get list ID
            int listId = connection.ExecuteScalar<int>(
                "SELECT id FROM lists_table WHERE name = @ListName",
                new { ListName = listName }
            );

            // Insert into list_movies_table
            connection.Execute(
                "INSERT INTO list_movies_table (list_id, movie_id) VALUES (@ListId, @MovieId)",
                new { ListId = listId, MovieId = movieId }
            );

            _ = LoadLists();
        }

        private static void CreateListsTable() {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS lists_table (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE
                );
            ");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS list_movies_table (
                    list_id INTEGER,
                    movie_id INTEGER,
                    FOREIGN KEY (list_id) REFERENCES lists_table(id),
                    FOREIGN KEY (movie_id) REFERENCES titles_table(id),
                    PRIMARY KEY (list_id, movie_id)
                );
            ");
        }

        private void AddListToTable(object sender, RoutedEventArgs e)
        {
            string listName = $"CustomList#{GetNextListId()}";

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            connection.Execute("INSERT INTO lists_table (name) VALUES (@name)", new { name = listName });

            Console.WriteLine("listname: " + listName + " created");

            CreateListUI(listName); // ✅ Ensure UI updates immediately
        }

        private static int GetNextListId()
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            return connection.ExecuteScalar<int>("SELECT COALESCE(MAX(id), 0) + 1 FROM lists_table");
        }

        private void CreateListUI(string listName)
        {
            var listsContainer = this.FindControl<StackPanel>("ListsContainer");
            if (listsContainer is null) return;
    
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
            StackPanel wrapper = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5
            };

            wrapper.Children.Add(dockPanel);
            wrapper.Children.Add(listContainer);

            listsContainer.Children.Add(wrapper); // ✅ ADD to ListsContainer
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