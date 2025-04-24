using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using CineLog.Views.Helper;
using Avalonia;
using System.Linq;
using Avalonia.Media;
using Avalonia.Controls.Documents;

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

            var imageSource = movie.GetImageSource();
            if (imageSource != null)
            {
                var imageBrush = new ImageBrush
                {
                    Source = imageSource,
                    Stretch = Stretch.UniformToFill,
                    Opacity = 0.75
                };
                this.FindControl<Border>("DetailsBorder")!.Background = imageBrush;
            }

            this.FindControl<TextBlock>("TitleText")!.Text = selectedTitle.Title_name;

            var basicInfoBlock = this.FindControl<TextBlock>("BasicInfo")!;
            basicInfoBlock.Inlines!.Clear();
            basicInfoBlock.Inlines.Add(new Run("🗓️") { FontFamily = new FontFamily("Noto Color Emoji") });

            basicInfoBlock.Inlines.Add(new Run($" {selectedTitle.Year_start}") { FontFamily = new FontFamily("Segoe UI") });

            if (selectedTitle.Year_end != null)
                basicInfoBlock.Inlines.Add(new Run($" - {selectedTitle.Year_end}") { FontFamily = new FontFamily("Segoe UI") });

            basicInfoBlock.Inlines.Add(new Run($" • ") { FontFamily = new FontFamily("Segoe UI") });
            basicInfoBlock.Inlines.Add(new Run("⭐") { FontFamily = new FontFamily("Noto Color Emoji") });
            basicInfoBlock.Inlines.Add(new Run($" {selectedTitle.Rating}") { FontFamily = new FontFamily("Segoe UI") });
            basicInfoBlock.Inlines.Add(new Run($" • ") { FontFamily = new FontFamily("Segoe UI") });
            basicInfoBlock.Inlines.Add(new Run("🕑") { FontFamily = new FontFamily("Noto Color Emoji") });

            if (!string.IsNullOrWhiteSpace(selectedTitle.Runtime))
                basicInfoBlock.Inlines.Add(new Run($" {selectedTitle.Runtime}") { FontFamily = new FontFamily("Segoe UI") });

            if (!string.IsNullOrWhiteSpace(selectedTitle.Season_count))
            {
                var seasonText = $" • {selectedTitle.Season_count} season";
                if (selectedTitle.Season_count != "1")
                    seasonText += "s";

                basicInfoBlock.Inlines.Add(new Run(seasonText) { FontFamily = new FontFamily("Segoe UI") });
            }

            this.FindControl<TextBlock>("DescriptionText")!.Text = selectedTitle.Plot ?? "";

            void InsertButtons(string panelName, string? items)
            {
                var panel = this.FindControl<WrapPanel>(panelName)!;
                panel.Children.Clear();
                var parent = panel.Parent as StackPanel;

                if (string.IsNullOrWhiteSpace(items))
                {
                    parent!.IsVisible = false;
                    return;
                }
                parent!.IsVisible = true;

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
            }

            InsertButtons("GenresButtons", selectedTitle.Genres);
            InsertButtons("CastButtons", selectedTitle.Stars);
            InsertButtons("WritersButtons", selectedTitle.Writers);
            InsertButtons("DirectorsButtons", selectedTitle.Directors);
            InsertButtons("CreatorsButtons", selectedTitle.Creators);
            InsertButtons("ProductionButtons", selectedTitle.Companies);

            var detailsBorder = this.FindControl<Border>("DetailsBorder")!;
            detailsBorder.ClipToBounds = true;
            detailsBorder.IsVisible = true;

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