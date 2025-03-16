using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.Documents;
using System.Data.SQLite;
using Dapper;

namespace CineLog.Views
{
    public partial class CollectionView : UserControl
    {
        private readonly HttpClient _httpClient = new();
        private WrapPanel? _moviesContainer;
        private ScrollViewer? _scrollViewer;
        private int _currentPage = 0;
        private const int PageSize = 50;
        private bool _isLoading = false;

        public CollectionView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Find controls after loading XAML
            _moviesContainer = this.FindControl<WrapPanel>("CollectionContainer");
            _scrollViewer = this.FindControl<ScrollViewer>("MovieScrollViewer");
            
            // Add scroll handler
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }
            
            // Initial load
            LoadNextPage();
        }

        private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (_scrollViewer == null || _isLoading) return;

            // Check if we've scrolled near the bottom
            double verticalOffset = _scrollViewer.Offset.Y;
            double viewportHeight = _scrollViewer.Viewport.Height;
            double extentHeight = _scrollViewer.Extent.Height;
            
            // If scrolled to within 200 pixels of the bottom, load more
            if (verticalOffset + viewportHeight + 200 >= extentHeight)
            {
                LoadNextPage();
            }
        }

        private async void LoadNextPage()
        {
            if (_isLoading || _moviesContainer == null) return;
            
            _isLoading = true;
            try
            {
                List<(string Title, string PosterUrl)> movies = GetMoviesFromDatabase(_currentPage, PageSize);
                
                foreach (var (title, posterUrl) in movies)
                {
                    await AddMovieToContainer(title, posterUrl);
                }
                
                _currentPage++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading movies: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task AddMovieToContainer(string title, string posterUrl)
        {
            if (_moviesContainer == null) return;

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

            contentPanel.Children.Add(imageBorder);
            contentPanel.Children.Add(movieTitle);
            movieBox.Child = contentPanel;

            _moviesContainer.Children.Add(movieBox);
        }

        private List<(string Title, string PosterUrl)> GetMoviesFromDatabase(int page, int pageSize)
        {
            string dbPath = "example.db";
            string connectionString = $"Data Source={dbPath};Version=3;";

            using var connection = new SQLiteConnection(connectionString);
            return connection.Query<(string, string)>(
                "SELECT title_name, poster_url FROM titles LIMIT @PageSize OFFSET @Offset", 
                new { PageSize = pageSize, Offset = page * pageSize }
            ).AsList();
        }

        private async Task LoadImageFromUrl(Image image, string url)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                image.Source = new Bitmap(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image from {url}: {ex.Message}");
                // Set a placeholder image if available
            }
        }
    }
}