using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using CineLog.Views.Helper;
using Avalonia;
using System.Linq;
using Avalonia.Media;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using System.Collections.Generic;
using System.IO;

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
        private Dictionary<string, Action<string, string>> _prefixHandlers = [];

        public CollectionView(string viewName)
        {
            this.viewName = viewName;
            InitializeComponent();
            Init();
        }

        public CollectionView()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            _moviesContainer = this.FindControl<WrapPanel>("CollectionWrapPanel")
                        ?? throw new NullReferenceException("WrapPanel not found in XAML");
            _scrollViewer = this.FindControl<ScrollViewer>("CollectionScrollViewer")
                        ?? throw new NullReferenceException("ScrollViewer not found in XAML");

            LoadNextPage();
            _scrollViewer.ScrollChanged += (sender, e) => OnScrollChanged();
            SortComboBox.SelectionChanged += SortComboBox_SelectionChanged;

            _prefixHandlers = new Dictionary<string, Action<string, string>>
            {
                ["in"] = HandleInterest,
                ["nm"] = HandleName,
                ["co"] = HandleCompany,
            };
        }

        private void LoadNextPage()
        {
            var sqlQuery = new DatabaseHandler.SQLQuerier {
                List_uuid = viewName,
                Limit = count,
                Offset = _currentOffset
            };

            var movies = DatabaseHandler.GetMovies(sqlQuery, filterSettings);

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

            void InsertButtons(string panelName, List<Tuple<string, string>>? items)
            {
                var panel = this.FindControl<WrapPanel>(panelName)!;
                panel.Children.Clear();
                var parent = panel.Parent as StackPanel;

                if (items == null || items.Count == 0)
                {
                    parent!.IsVisible = false;
                    return;
                }

                parent!.IsVisible = true;

                foreach (var (id, name) in items)
                {
                    Button button = new()
                    {
                        Content = name,
                        FontSize = 12,
                        Padding = new Thickness(4, 2),
                        Margin = new Thickness(4, 2),
                        CornerRadius = new CornerRadius(8),
                        Tag = id
                    };
                    button.Click += FilterBySpecific;
                    panel.Children.Add(button);
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

        private void RenewList()
        {
            _moviesContainer?.Children.Clear();
            _currentOffset = 0;
        }

        #region Buttons

        private void ApplyFilter()
        {
            RenewList();
            LoadNextPage();
        }

        private void CloseDetails(object? sender, RoutedEventArgs e)
        {
            this.FindControl<Border>("DetailsBorder")!.IsVisible = false;
        }

        private void AddToCalendar(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string title_id)
            {
                var scheduleList = DatabaseHandler.GetSchedule(title_id);
                Console.WriteLine("shcedule listL: " + scheduleList);
                CalendarView.AddMovieToCalendar(scheduleList, title_id);
            }
        }

        private async void OpenFilterModal(object? sender, RoutedEventArgs e)
        {
            var modal = new FilterModal(filterSettings);
            if (VisualRoot is not Window window) return;

            var result = await modal.ShowDialog<DatabaseHandler.FilterSettings?>(window);

            if (result != null)
            {
                filterSettings = result;
                UpdateFilterChip();
                ApplyFilter();
            }
        }

        private void UpdateFilterChip()
        {
            FilterChipPanel.Children.Clear();

            foreach (var genre in filterSettings.Genre!) AddFilterChip("Genre", genre.Item2);
            foreach (var company in filterSettings.Company!) AddFilterChip("Company", company.Item2);
            foreach (var name in filterSettings.Name!) AddFilterChip("Name", name.Item2);

            if (!string.IsNullOrWhiteSpace(filterSettings.Type)) AddFilterChip("Type", filterSettings.Type);
            if (!string.IsNullOrWhiteSpace(filterSettings.SearchTerm)) AddFilterChip("SearchTerm", filterSettings.SearchTerm);

            if (filterSettings.MinRating != 0) AddFilterChip("MinRating", filterSettings.MinRating.ToString()!);
            if (filterSettings.MaxRating != 10) AddFilterChip("MaxRating", filterSettings.MaxRating.ToString()!);

            if (filterSettings.YearStart != 1874) AddFilterChip("YearStart", filterSettings.YearStart.ToString()!);
            if (filterSettings.YearEnd != DateTime.Now.Year + 1) AddFilterChip("YearEnd", filterSettings.YearEnd.ToString()!);
        }

        private void AddFilterChip(string source, string filterText)
        {
            var tag = (source + filterText).ToString();

            foreach (var child in FilterChipPanel.Children)
            {
                if (child is Control control && control.Tag?.ToString() == tag) return;
            }

            var border = new Border
            {
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(5),
                Padding = new Thickness(8, 4, 8, 4),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = tag,
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new Button
                        {
                            Content = "✖",
                            Width = 20,
                            Height = 20,
                            Padding = new Thickness(0),
                            FontSize = 12,
                            VerticalAlignment = VerticalAlignment.Center,
                            Tag = new FilterChipTag(source, filterText),
                            Background = Brushes.Transparent,
                            BorderBrush = Brushes.Transparent
                        },
                        new TextBlock
                        {
                            Text = source + ": " + filterText,
                            Margin = new Thickness(0, 0, 5, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            };

            var button = (border.Child! as StackPanel)!.Children[0] as Button;
            button!.Click += RemoveFilterChip!;

            FilterChipPanel.Children.Add(border);
        }

        private void RemoveFilterChip(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Parent is StackPanel stack &&
                stack.Parent is Border border &&
                button.Tag is FilterChipTag tag)
            {
                FilterChipPanel.Children.Remove(border);
                string source = tag.Source;
                string value = tag.Value;

                switch (source)
                {
                    case "Genre":
                        filterSettings.Genre?.RemoveAll(g => g.Item2 == value);
                        break;
                    case "Company":
                        filterSettings.Company?.RemoveAll(c => c.Item2 == value);
                        break;
                    case "Name":
                        filterSettings.Name?.RemoveAll(c => c.Item2 == value);
                        break;
                    case "Type":
                        if (filterSettings.Type == value) filterSettings.Type = null;
                        break;
                    case "SearchTerm":
                        if (filterSettings.SearchTerm == value) filterSettings.SearchTerm = null;
                        break;
                    case "MinRating":
                        filterSettings.MinRating = 0;
                        break;
                    case "MaxRating":
                        filterSettings.MaxRating = 10;
                        break;
                    case "YearStart":
                        filterSettings.YearStart = 1874;
                        break;
                    case "YearEnd":
                        filterSettings.YearEnd = DateTime.Now.Year + 1;
                        break;
                }

                UpdateFilterChip();
                ApplyFilter();
            }
        }

        private void SortComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
            {
                filterSettings.SortBy = item.Tag!.ToString();
                ApplyFilter();
            }
        }

        private void FilterBySpecific(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn) {
                string id = btn.Tag!.ToString()!;
                string prefix = id[..2];
                string name = btn.Content!.ToString()!;

                if (_prefixHandlers.TryGetValue(prefix, out var handler))
                {
                    handler(name, id);
                    UpdateFilterChip();
                    ApplyFilter();
                }
                else
                {
                    Console.WriteLine("Unknown button type: " + id);
                    return;
                }
            }
        }

        private void HandleInterest(string name, string id)
        {
            filterSettings.Genre!.Add(new Tuple<string, string>(id, name));
        }

        private void HandleName(string name, string id)
        {
            filterSettings.Name!.Add(new Tuple<string, string>(id, name));
        }

        private void HandleCompany(string name, string id)
        {
            filterSettings.Company!.Add(new Tuple<string, string>(id, name));
        }

        #endregion
    }

    public class FilterChipTag(string source, string value)
    {
        public string Source { get; set; } = source;
        public string Value { get; set; } = value;
    }
}