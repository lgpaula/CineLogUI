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
using CineLog.ViewModels;

namespace CineLog.Views
{
    public partial class HomeView : UserControl
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
        private StackPanel? _moviesContainer;

        public HomeView()
        {
            InitializeComponent();
            LoadMovies();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            Console.WriteLine("home");
            _moviesContainer = this.FindControl<StackPanel>("MoviesContainer");
        }

        private async void LoadMovies()
        {
            List<(string Title, string PosterUrl)> movies = GetMoviesFromDatabase();

            foreach (var (title, posterUrl) in movies)
            {
                Border movieBox = new()
                {
                    BorderBrush = Brushes.White,
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

                // Asynchronously load image from URL
                await LoadImageFromUrl(movieImage, posterUrl);

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

                contentPanel.Children.Add(movieImage);
                contentPanel.Children.Add(movieTitle);
                movieBox.Child = contentPanel;
                
                _moviesContainer?.Children.Add(movieBox);
            }
        }

        // Load image from URL asynchronously
        private async Task LoadImageFromUrl(Image imageControl, string imageUrl)
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

        private List<(string Title, string PosterUrl)> GetMoviesFromDatabase()
        {
            List<(string, string)> movies = [];

            string dbPath = "example.db"; // Ensure the path is correct
            string connectionString = $"Data Source={dbPath};Version=3;";

            using (SQLiteConnection conn = new(connectionString))
            {
                conn.Open();
                string sql = "SELECT title_name, poster_url FROM titles"; // Adjust table and column name
                using SQLiteCommand cmd = new(sql, conn);
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string title = reader.GetString(0);
                    string posterUrl = reader.GetString(1);
                    movies.Add((title, posterUrl));
                }
            }

            return movies;
        }

        private void ClickHandler(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string viewName)
            {
                ViewModel.HandleButtonClick(viewName);
            }
        }
    }
}
