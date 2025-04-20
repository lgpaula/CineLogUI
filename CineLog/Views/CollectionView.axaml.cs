using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using CineLog.Views.Helper;
using Avalonia.Media;
using Avalonia;
using System.Linq;

namespace CineLog.Views
{
    public partial class CollectionView : UserControl
    {
        public string viewName = string.Empty;
        private int _currentOffset = 0;
        private const int count = 50;
        private WrapPanel? _moviesContainer;
        private ScrollViewer? _scrollViewer;
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
                movieButton.Click += MovieButton_Click;
                _moviesContainer?.Children.Add(movieButton);
            }

            _currentOffset += count;
        }

        private void OnScrollChanged()
        {
            if (_scrollViewer?.Offset.Y + _scrollViewer?.Viewport.Height >= _scrollViewer?.Extent.Height - 100)
            {
                LoadNextPage();
            }
        }

        private async void MovieButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string movieId)
            {
                var selectedTitle = await DatabaseHandler.GetTitleInfo(movieId);
                ShowMovieDetails(selectedTitle);
            }
        }

        private void ShowMovieDetails(DatabaseHandler.TitleInfo selectedTitle)
        {
            var movie = new Movie(selectedTitle.Title_Id);

            // Swap poster with image with rounded corners
            var posterImage = movie.GetImageBorder();
            posterImage.Height = 100;
            posterImage.Width = 70;
            this.FindControl<Border>("PosterButton")!.Child = posterImage;

            // Set basic text fields
            this.FindControl<TextBlock>("TitleText")!.Text = movie.Title;
            this.FindControl<TextBlock>("YearStartText")!.Text = selectedTitle.Year_start?.ToString() ?? "";
            this.FindControl<TextBlock>("YearEndText")!.Text = selectedTitle.Year_end?.ToString() ?? "";
            this.FindControl<TextBlock>("RatingText")!.Text = selectedTitle.Rating?.ToString() ?? "";
            this.FindControl<TextBlock>("RuntimeText")!.Text = selectedTitle.Runtime != null ? $"{selectedTitle.Runtime}" : "";
            this.FindControl<TextBlock>("SeasonCountText")!.Text = selectedTitle.Season_count?.ToString() ?? "";
            this.FindControl<TextBlock>("DescriptionText")!.Text = selectedTitle.Plot ?? "";

            // Helper to insert button list into TextBlock's parent
            void InsertButtons(string name, string? items)
            {
                var textBlock = this.FindControl<TextBlock>(name)!;
                var parent = textBlock.Parent as Panel;

                // Remove the existing TextBlock (used for layout only)
                parent?.Children.Remove(textBlock);

                if (!string.IsNullOrWhiteSpace(items) && parent != null)
                {
                    var panel = new WrapPanel();
                    var entries = items.Split(',')
                                    .Select(s => s.Trim())
                                    .Where(s => !string.IsNullOrEmpty(s));

                    foreach (var item in entries)
                    {
                        panel.Children.Add(new Button
                        {
                            Content = item,
                            FontSize = 12,
                            Padding = new Thickness(4, 2),
                            Margin = new Thickness(4, 2),
                            CornerRadius = new CornerRadius(8)
                        });
                    }

                    parent.Children.Add(panel);
                }
            }

            InsertButtons("GenresText", selectedTitle.Genres);
            InsertButtons("CompaniesText", selectedTitle.Companies);
            InsertButtons("StarsText", selectedTitle.Stars);
            InsertButtons("DirectorsText", selectedTitle.Directors);
            InsertButtons("WritersText", selectedTitle.Writers);
            InsertButtons("CreatorsText", selectedTitle.Creators);

            // Reveal the details panel
            this.FindControl<Border>("DetailsBorder")!.IsVisible = true;

            // Set calendar tag for later use
            this.FindControl<Button>("Calendar")!.Tag = movie.Id;
        }

        private void CloseDetails(object? sender, RoutedEventArgs e)
        {
            this.FindControl<Border>("DetailsBorder")!.IsVisible = false;
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

        private void AddToCalendar(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string title_id)
            {
                var scheduleList = DatabaseHandler.GetSchedule(title_id);
                CalendarView.AddMovieToCalendar(scheduleList, title_id);
            }
        }
    }
}