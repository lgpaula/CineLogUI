using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Net.Http;
using System.Collections.Generic;

namespace CineLog.Views
{
    public partial class CollectionView : UserControl
    {
        private readonly HttpClient _httpClient = new();
        private WrapPanel? _moviesContainer;
        private string? viewName;

        public CollectionView(string viewName)
        {
            Console.WriteLine($"Loading {viewName} view");
            this.viewName = viewName;
            InitializeComponent();
        }

        public CollectionView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            _moviesContainer = this.FindControl<WrapPanel>("CollectionWrapPanel");
            
            // Set up sizing for the container
            if (_moviesContainer != null)
            {
                // Make sure the WrapPanel takes up available width
                _moviesContainer.Width = Bounds.Width;
            }
            
            // Initial load
            LoadNextPage();
        }

        private void LoadNextPage()
        {
            if (_moviesContainer == null) return;
                        
            try
            {
                List<Movie> movies = DatabaseHandler.GetMovies(viewName);
                Console.WriteLine($"Loaded {movies.Count} movies");
                
                foreach (var movie in movies)
                {
                    Button movieButton = movie.CreateMovieButton(_httpClient);
                    _moviesContainer.Children.Add(movieButton);
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading movies: {ex.Message}");
            }
        }
    }
}