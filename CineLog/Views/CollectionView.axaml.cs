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
        private const int PageSize = 100;
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
            
            // Set up sizing for the container
            if (_moviesContainer != null)
            {
                // Make sure the WrapPanel takes up available width
                _moviesContainer.Width = Bounds.Width;
            }
            
            // Add scroll handler
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }
            
            // Initial load
            LoadNextPage();
            
            // Subscribe to size changes
            this.LayoutUpdated += (s, e) => {
                if (_moviesContainer != null && Math.Abs(_moviesContainer.Width - Bounds.Width) > 1)
                {
                    _moviesContainer.Width = Bounds.Width;
                }
            };
        }

        private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (_scrollViewer == null || _isLoading) return;
            
            // Check if we've scrolled near the bottom
            double verticalOffset = _scrollViewer.Offset.Y;
            double viewportHeight = _scrollViewer.Viewport.Height;
            double extentHeight = _scrollViewer.Extent.Height;
            
            Console.WriteLine($"Scroll - Offset: {verticalOffset}, Viewport: {viewportHeight}, Extent: {extentHeight}");
            
            // If scrolled to within 200 pixels of the bottom, load more
            if (verticalOffset + viewportHeight + 200 >= extentHeight)
            {
                LoadNextPage();
            }
        }

        private async void LoadNextPage()
        {
            if (_isLoading || _moviesContainer == null) return;
            
            Console.WriteLine($"Loading page {_currentPage}");
            _isLoading = true;
            
            try
            {
                List<(string Title, string PosterUrl)> movies = GetMoviesFromDatabase(_currentPage, PageSize);
                Console.WriteLine($"Loaded {movies.Count} movies");
                
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

            // Create a Button to make the item clickable
            Button movieButton = new()
            {
                Padding = new Thickness(0),
                Margin = new Thickness(10),
                Width = 150,
                Height = 250,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            Border movieBox = new()
            {
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5),
                Background = new SolidColorBrush(Color.Parse("#222222"))
            };

            StackPanel contentPanel = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5
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
                CornerRadius = new CornerRadius(5),
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
                MaxLines = 2,
                Margin = new Thickness(0, 5, 0, 0)
            };

            contentPanel.Children.Add(imageBorder);
            contentPanel.Children.Add(movieTitle);
            movieBox.Child = contentPanel;
            movieButton.Content = movieBox;

            // Add click handler
            movieButton.Click += (s, e) => {
                Console.WriteLine($"Movie clicked: {title}");
                // Here you would navigate to the movie details
            };

            _moviesContainer.Children.Add(movieButton);
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
                // Set a placeholder image 
                image.Source = null;
                // If you have a placeholder image, you would set it here
            }
        }
    }
}