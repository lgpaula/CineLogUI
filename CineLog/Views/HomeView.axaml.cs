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
using Avalonia.Input;

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
                try
                {
                    CreateListUI(listName);
                    var movies = GetMoviesFromList(listName);
                    await LoadCollection(listName, movies);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading {listName}: {ex.Message}");
                }
            }
        }

        private static List<string> GetListsFromDatabase()
        {
            List<string> lists = [];
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            using var command = new SQLiteCommand("SELECT name FROM lists_table;", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lists.Add(reader.GetString(0));
            }

            return lists;
        }

        private static bool IsMovieInList(string listName, string movieId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT COUNT(*) 
                FROM list_movies_table lm
                JOIN lists_table l ON lm.list_id = l.id
                WHERE l.name = @ListName AND lm.movie_id = @MovieId";

            int count = connection.ExecuteScalar<int>(query, new { ListName = listName, MovieId = movieId });
            return count > 0;
        }

        private async Task LoadCollection(string panelName, List<(string MovieId, string Title, string PosterUrl)> titles)
        {
            StackPanel? panel = this.FindControl<StackPanel>(panelName);

            foreach (var (movieId, title, posterUrl) in titles)
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
                    Width = 130,
                    Height = 180
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

                // Flyout for the ellipsis button
                Flyout flyout = new();
                Border flyoutBorder = new()
                {
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(5)
                };

                StackPanel flyoutPanel = new()
                {
                    Spacing = 5,
                    Width = 200
                };

                // Create the "Add to list" expander
                var addToListExpander = new Expander
                {
                    Header = new TextBlock 
                    {
                        Text = "Add to List",
                        Foreground = Brushes.White
                    },
                    IsExpanded = true,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Foreground = Brushes.White,
                    Background = Brushes.Transparent
                };

                // Create a panel for checkboxes
                var checkboxesPanel = new StackPanel
                {
                    Margin = new Thickness(5)
                };
                
                // Add the checkboxes to the panel
                foreach (var listName in GetListsFromDatabase())
                {
                    // Create a wrapper panel for checkbox and label
                    var checkboxWrapper = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(5, 2, 5, 2)
                    };
                    
                    // Create a checkbox with explicit styling
                    CheckBox listCheckBox = new()
                    {
                        Name = $"checkbox_{listName.Replace(" ", "X")}",  // Unique name for the checkbox
                        Foreground = Brushes.White,
                        Background = Brushes.Transparent,
                        BorderBrush = Brushes.White,
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(0, 0, 5, 0),
                        IsChecked = true,
                        MinWidth = 16,
                        MinHeight = 16,
                    };
                    
                    // Create a separate text label
                    TextBlock checkBoxLabel = new()
                    {
                        Text = listName,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    
                    // Add both to the wrapper panel
                    checkboxWrapper.Children.Add(listCheckBox);
                    checkboxWrapper.Children.Add(checkBoxLabel);
                    
                    // Handle checkbox toggle logic
                    listCheckBox.IsCheckedChanged += (s, e) =>
                    {
                        listCheckBox.InvalidateVisual();

                        if (listCheckBox.IsChecked == true)
                        {
                            listCheckBox.Foreground = Brushes.LightGreen;
                            // Uncomment and implement this method
                            // AddMovieToList(listName, movieId);
                            Console.WriteLine("Adding movie to list: " + listName);
                        }
                        else
                        {
                            listCheckBox.Foreground = Brushes.White;
                            // Uncomment and implement this method
                            // RemoveMovieFromList(listName, movieId);
                            Console.WriteLine("Removing movie from list: " + listName);
                        }
                    };
                    
                    // Add the wrapper to the main panel
                    checkboxesPanel.Children.Add(checkboxWrapper);
                }

                // Set the checkboxes panel as the content of the expander
                addToListExpander.Content = checkboxesPanel;
                flyoutPanel.Children.Add(addToListExpander);

                // Wrap the flyoutPanel inside the flyoutBorder
                flyoutBorder.Child = flyoutPanel;
                flyout.Content = flyoutBorder;

                // Ellipsis button with the Flyout
                Button ellipsisButton = new()
                {
                    Content = "⋮",
                    FontSize = 16,
                    Foreground = Brushes.White,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Flyout = flyout
                };

                DockPanel headerPanel = new();
                DockPanel.SetDock(ellipsisButton, Dock.Right);
                headerPanel.Children.Add(ellipsisButton);

                contentPanel.Children.Add(headerPanel);
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

        private List<(string id, string Title, string PosterUrl)> GetMoviesFromCollection()
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "SELECT title_id, title_name, poster_url FROM titles_table";
            return connection.Query<(string, string, string)>(query).AsList();
        }

        private static List<(string id, string Title, string PosterUrl)> GetMoviesFromList(string listName)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT t.title_id, t.title_name, t.poster_url
                FROM titles_table t
                JOIN list_movies_table lm ON t.title_id = lm.movie_id
                JOIN lists_table l ON lm.list_id = l.id
                WHERE l.name = @ListName";

            return connection.Query<(string, string, string)>(query, new { ListName = listName }).AsList();
        }

        private void AddMovieToList(string listName, string movieId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Get list ID
                int listId = connection.ExecuteScalar<int>(
                    "SELECT id FROM lists_table WHERE name = @ListName",
                    new { ListName = listName }
                );

                // Check if movie is already in the list
                int exists = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM list_movies_table WHERE list_id = @ListId AND movie_id = @MovieId",
                    new { ListId = listId, MovieId = movieId }
                );

                if (exists == 0)
                {
                    // Insert into list_movies_table
                    connection.Execute(
                        "INSERT INTO list_movies_table (list_id, movie_id) VALUES (@ListId, @MovieId)",
                        new { ListId = listId, MovieId = movieId }
                    );
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error adding movie to list: {ex.Message}");
            }
        }

        private void RemoveMovieFromList(string listName, string movieId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                string query = @"
                    DELETE FROM list_movies_table 
                    WHERE list_id IN (SELECT id FROM lists_table WHERE name = @ListName)
                    AND movie_id = @MovieId";

                connection.Execute(query, new { ListName = listName, MovieId = movieId });
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error removing movie from list: {ex.Message}");
            }
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

            CreateListUI(listName);
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

            listsContainer.Children.Add(wrapper);
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