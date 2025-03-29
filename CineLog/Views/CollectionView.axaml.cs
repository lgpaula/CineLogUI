using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace CineLog.Views
{
    public partial class CollectionView : UserControl
    {
        public string viewName = string.Empty;
        private int _currentOffset = 0;
        private const int count = 50;
        private WrapPanel ?_moviesContainer;
        private ScrollViewer ?_scrollViewer;
        private DatabaseHandler.FilterSettings filterSettings = new();

        public CollectionView(string viewName)
        {
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

            _moviesContainer = this.FindControl<WrapPanel>("CollectionWrapPanel")
                        ?? throw new NullReferenceException("WrapPanel not found in XAML");
            _scrollViewer = this.FindControl<ScrollViewer>("CollectionScrollViewer") 
                        ?? throw new NullReferenceException("ScrollViewer not found in XAML");

            LoadNextPage();

            _scrollViewer.ScrollChanged += (sender, e) => OnScrollChanged();
        }

        private void LoadNextPage()
        {
            var movies = DatabaseHandler.GetMovies(viewName, count, _currentOffset, filterSettings);

            if (movies.Count == 0) return;

            foreach (var movie in movies)
            {
                var movieButton = movie.CreateMovieButton();
                _moviesContainer?.Children.Add(movieButton);
            }

            Console.WriteLine($"Loaded {count} more movies (starting from offset {_currentOffset})");

            _currentOffset += count;
        }

        private void OnScrollChanged()
        {
            // Console.WriteLine($"Offset.Y: {scrollViewer.Offset.Y}, window.Height: {scrollViewer.Viewport.Height}, window.Width: {scrollViewer.Extent.Height}");
            if (_scrollViewer?.Offset.Y + _scrollViewer?.Viewport.Height >= _scrollViewer?.Extent.Height - 100)
            {
                Console.WriteLine("Loading more items...");
                LoadNextPage();
            }
        }

        #region Buttons
        
        private void ApplyFilter(object? sender, RoutedEventArgs e)
        {
            _moviesContainer?.Children.Clear();
            _currentOffset = 0; // Reset offset when applying a new filter

            // Read the Rating RangeSlider values (e.g., minimum and maximum selected ratings)
            var ratingSlider = this.FindControl<RangeSlider.Avalonia.Controls.RangeSlider>("RatingSlider");
            var ratingRange = ratingSlider != null ? 
                            new Tuple<float, float>((float)ratingSlider.LowerSelectedValue, (float)ratingSlider.UpperSelectedValue) : 
                            null;

            // Read selected Genres (e.g., checked checkboxes in GenreCheckBoxPanel)
            var selectedGenres = new List<string>();
            var genrePanel = this.FindControl<WrapPanel>("GenreCheckBoxPanel");
            if (genrePanel != null)
            {
                foreach (var child in genrePanel.Children)
                {
                    if (child is CheckBox checkBox && checkBox.IsChecked == true && checkBox.Content != null)
                    {
                        selectedGenres.Add(checkBox.Content.ToString());
                    }
                }
            }

            // Read the Year RangeSlider values (e.g., minimum and maximum selected years)
            var yearSlider = this.FindControl<RangeSlider.Avalonia.Controls.RangeSlider>("YearSlider");
            var yearRange = yearSlider != null ? 
                            new Tuple<int, int>((int)yearSlider.LowerSelectedValue, (int)yearSlider.UpperSelectedValue) : 
                            null;

            // Read Company TextBox
            string? company = this.FindControl<TextBox>("CompanyTextBox")?.Text;

            // Read Title Type (Movie or Series)
            var selectedTypes = new List<string>();
            var typePanel = this.FindControl<WrapPanel>("TitleTypePanel");
            if (typePanel != null)
            {
                foreach (var child in typePanel.Children)
                {
                    if (child is CheckBox checkBox && checkBox.IsChecked == true && checkBox.Tag is string typeTag)
                        selectedTypes.Add(typeTag);
                }
            }

            // Update the filter settings
            filterSettings = new DatabaseHandler.FilterSettings
            {
                Rating = ratingRange,
                Genre = selectedGenres,
                Year = yearRange,
                Company = company,
                Type = selectedTypes.Count > 0 ? string.Join(",", selectedTypes) : null
            };

            // Reload the movies based on the new filter
            LoadNextPage();

            Console.WriteLine("Filtering movies...");
        }

        #endregion

    }
}