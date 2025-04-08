using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using CineLog.Views.Helper;
using CineLog.ViewModels;

namespace CineLog.Views
{
    public partial class CollectionView : UserControl
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
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
                movieButton.Tag = movie.Id;
                movieButton.Click += ViewChanger;
                _moviesContainer?.Children.Add(movieButton);
            }

            Console.WriteLine($"Loaded {count} more movies (starting from offset {_currentOffset})");

            _currentOffset += count;
        }

        private void OnScrollChanged()
        {
            if (_scrollViewer?.Offset.Y + _scrollViewer?.Viewport.Height >= _scrollViewer?.Extent.Height - 100)
            {
                LoadNextPage();
            }
        }

        #region Buttons
        
        private void ApplyFilter(object? sender, RoutedEventArgs e)
        {
            _moviesContainer?.Children.Clear();
            _currentOffset = 0;

            float minRating = 0.0f;
            float maxRating = 10.0f;

            // Read Rating values
            var minRatingBox = this.FindControl<TextBox>("MinRating");
            var maxRatingBox = this.FindControl<TextBox>("MaxRating");
            if (minRatingBox != null && float.TryParse(minRatingBox.Text, out float minRatingParsed)) minRating = minRatingParsed;
            if (maxRatingBox != null && float.TryParse(maxRatingBox.Text, out float maxratingParsed)) maxRating = maxratingParsed;

            // Read selected Genres
            var selectedGenres = new List<string>();
            var genrePanel = this.FindControl<WrapPanel>("GenreCheckBoxPanel");
            if (genrePanel != null)
            {
                foreach (var child in genrePanel.Children)
                {
                    if (child is CheckBox checkBox && checkBox.IsChecked == true && checkBox.Content != null)
                    {
                        var content = checkBox.Content.ToString();
                        if (!string.IsNullOrEmpty(content))
                            selectedGenres.Add(content);
                    }
                }
            }

            // Read Year values
            int yearStart = 1874;
            int yearEnd = DateTime.Now.Year;

            var minYearBox = this.FindControl<TextBox>("MinYear");
            var maxYearBox = this.FindControl<TextBox>("MaxYear");
            if (minYearBox != null && int.TryParse(minYearBox.Text, out int minYearParsed)) yearStart = minYearParsed;
            if (maxYearBox != null && int.TryParse(maxYearBox.Text, out int maxYearParsed)) yearEnd = maxYearParsed;

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
                MinRating = minRating,
                MaxRating = maxRating,
                Genre = selectedGenres,
                YearStart = yearStart,
                YearEnd = yearEnd,
                Company = company,
                Type = selectedTypes.Count > 0 ? string.Join(",", selectedTypes) : null
            };

            LoadNextPage();
        }

        #endregion

        private void ViewChanger(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string viewName)
            {
                ViewModel?.HandleButtonClick(viewName);
            }
        }

    }
}