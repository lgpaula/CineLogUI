using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Net.Http;
using System.Data.SQLite;
using System.Collections.Generic;
using Dapper;
using System.Linq;

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

        public CollectionView(string viewName)
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
            LayoutUpdated += (s, e) => {
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
                List<Movie> movies = GetMoviesFromDatabase(_currentPage, PageSize);
                Console.WriteLine($"Loaded {movies.Count} movies");
                
                foreach (var movie in movies)
                {
                    Button movieButton = await movie.CreateMovieButton(_httpClient);
                    _moviesContainer.Children.Add(movieButton);
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

        private static List<Movie> GetMoviesFromDatabase(int page, int pageSize)
        {
            string dbPath = "example.db";
            string connectionString = $"Data Source={dbPath};Version=3;";
            using var connection = new SQLiteConnection(connectionString);

            string query = "SELECT title_id, title_name, poster_url FROM titles_table LIMIT @PageSize OFFSET @Offset";

            var result = connection.Query<(string, string, string)>(query, new { PageSize = pageSize, Offset = page * pageSize })
                        .Select(t => new Movie(t.Item1, t.Item2, t.Item3))
                        .ToList();

            return result;
        }
    }
}