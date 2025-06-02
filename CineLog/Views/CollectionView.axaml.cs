using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using CineLog.Views.Helper;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using System.Collections.Generic;

namespace CineLog.Views
{
    public partial class CollectionView : UserControl
    {
        private readonly string _viewName = string.Empty;
        private int _currentOffset;
        private const int Count = 50;
        private WrapPanel? _moviesContainer;
        private ScrollViewer? _scrollViewer;
        private DatabaseHandler.FilterSettings _filterSettings = new();
        private Dictionary<string, Action<string, string>> _prefixHandlers = [];

        public CollectionView(string viewName)
        {
            _viewName = viewName;
            InitializeComponent();
            AttachedToVisualTree += OnLoaded;
        }

        public CollectionView()
        {
            InitializeComponent();
            AttachedToVisualTree += OnLoaded;
        }

        private void OnLoaded(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _moviesContainer = this.FindControl<WrapPanel>("CollectionWrapPanel")
                        ?? throw new NullReferenceException("WrapPanel not found in XAML");
            _scrollViewer = this.FindControl<ScrollViewer>("CollectionScrollViewer")
                        ?? throw new NullReferenceException("ScrollViewer not found in XAML");

            LoadNextPage();
            _scrollViewer.ScrollChanged += (_, _) => OnScrollChanged();
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
            var sqlQuery = new DatabaseHandler.SqlQuerier {
                ListUuid = _viewName,
                Limit = Count,
                Offset = _currentOffset
            };

            var movies = DatabaseHandler.GetMovies(sqlQuery, _filterSettings);

            if (movies.Count == 0) return;

            foreach (var movie in movies)
            {
                var movieButton = movie.CreateMovieButton();
                movieButton.Tag = movie.Id;
                movieButton.Click += MovieButton_Click;
                _moviesContainer?.Children.Add(movieButton);
            }

            _currentOffset += Count;
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
            if (sender is not Button { Tag: string movieId }) return;

            var selectedTitle = await DatabaseHandler.GetTitleInfo(movieId);
            ShowMovieDetails(selectedTitle);
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
            return;

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
            if (sender is not Button { Tag: string titleId }) return;

            var scheduleList = DatabaseHandler.GetSchedule(titleId);
            CalendarView.AddMovieToCalendar(scheduleList, titleId);
            
            EventAggregator.Instance.Publish(new NotificationEvent { Message = $"✅ Added {titleId} to calendar." });
        }

        private async void OpenFilterModal(object? sender, RoutedEventArgs e)
        {
            var modal = new FilterModal(_filterSettings);
            if (VisualRoot is not Window window) return;

            var result = await modal.ShowDialog<DatabaseHandler.FilterSettings?>(window);
            if (result == null) return;

            _filterSettings = result;
            UpdateFilterChip();
            ApplyFilter();
        }

        private void UpdateFilterChip()
        {
            FilterChipPanel.Children.Clear();

            foreach (var genre in _filterSettings.Genre!) AddFilterChip("Genre", genre.Item2);
            foreach (var company in _filterSettings.Company!) AddFilterChip("Company", company.Item2);
            foreach (var name in _filterSettings.Name!) AddFilterChip("Name", name.Item2);

            if (!string.IsNullOrWhiteSpace(_filterSettings.Type)) AddFilterChip("Type", _filterSettings.Type);
            if (!string.IsNullOrWhiteSpace(_filterSettings.SearchTerm)) AddFilterChip("SearchTerm", _filterSettings.SearchTerm);

            if (_filterSettings.MinRating != 0) AddFilterChip("MinRating", _filterSettings.MinRating.ToString()!);
            if (_filterSettings.MaxRating != 10) AddFilterChip("MaxRating", _filterSettings.MaxRating.ToString()!);

            if (_filterSettings.YearStart != 1874) AddFilterChip("YearStart", _filterSettings.YearStart.ToString()!);
            if (_filterSettings.YearEnd != DateTime.Now.Year + 1) AddFilterChip("YearEnd", _filterSettings.YearEnd.ToString()!);
        }

        private void AddFilterChip(string source, string filterText)
        {
            var tag = source + filterText;

            foreach (var child in FilterChipPanel.Children)
            {
                if (child is not null && child.Tag?.ToString() == tag) return;
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
            if (sender is not Button { Parent: StackPanel { Parent: Border border }, Tag: FilterChipTag tag }) return;
            
            FilterChipPanel.Children.Remove(border);
            var source = tag.Source;
            var value = tag.Value;

            switch (source)
            {
                case "Genre":
                    _filterSettings.Genre?.RemoveAll(g => g.Item2 == value);
                    break;
                case "Company":
                    _filterSettings.Company?.RemoveAll(c => c.Item2 == value);
                    break;
                case "Name":
                    _filterSettings.Name?.RemoveAll(c => c.Item2 == value);
                    break;
                case "Type":
                    if (_filterSettings.Type == value) _filterSettings.Type = null;
                    break;
                case "SearchTerm":
                    if (_filterSettings.SearchTerm == value) _filterSettings.SearchTerm = null;
                    break;
                case "MinRating":
                    _filterSettings.MinRating = 0;
                    break;
                case "MaxRating":
                    _filterSettings.MaxRating = 10;
                    break;
                case "YearStart":
                    _filterSettings.YearStart = 1874;
                    break;
                case "YearEnd":
                    _filterSettings.YearEnd = DateTime.Now.Year + 1;
                    break;
            }

            UpdateFilterChip();
            ApplyFilter();
        }

        private void SortComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox { SelectedItem: ComboBoxItem item }) return;

            _filterSettings.SortBy = item.Tag!.ToString();
            ApplyFilter();
        }

        private void FilterBySpecific(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var id = btn.Tag!.ToString()!;
            var prefix = id[..2];
            var name = btn.Content!.ToString()!;

            if (_prefixHandlers.TryGetValue(prefix, out var handler))
            {
                handler(name, id);
                UpdateFilterChip();
                ApplyFilter();
            }
            else
            {
                Console.WriteLine("Unknown button type: " + id);
            }
        }

        private void HandleInterest(string name, string id)
        {
            _filterSettings.Genre!.Add(new Tuple<string, string>(id, name));
        }

        private void HandleName(string name, string id)
        {
            _filterSettings.Name!.Add(new Tuple<string, string>(id, name));
        }

        private void HandleCompany(string name, string id)
        {
            _filterSettings.Company!.Add(new Tuple<string, string>(id, name));
        }

        #endregion
    }

    public class FilterChipTag(string source, string value)
    {
        public string Source { get; } = source;
        public string Value { get; } = value;
    }
}