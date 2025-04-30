using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using CineLog.Views.Helper;
using Avalonia;
using System.Linq;
using Avalonia.Media;
using Avalonia.Controls.Documents;
using Avalonia.Layout;

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
                // UpdateFilterChip();
                ApplyFilter();
            }
        }

        // private void UpdateFilterChip()
        // {
            // foreach (var setting in filterSettings.)
            // {
            //     AddFilterChip(setting);
            // }
        // }

        // private void AddFilterChip(string filterText)
        // {
        //     var border = new Border
        //     {
        //         Background = Brushes.LightGray,
        //         CornerRadius = new CornerRadius(10),
        //         Margin = new Thickness(5),
        //         Padding = new Thickness(8, 4, 8, 4),
        //         Child = new StackPanel
        //         {
        //             Orientation = Orientation.Horizontal,
        //             Children =
        //             {
        //                 new Button
        //                 {
        //                     Content = "✖",
        //                     Width = 20,
        //                     Height = 20,
        //                     Padding = new Thickness(0),
        //                     FontSize = 12,
        //                     VerticalAlignment = VerticalAlignment.Center,
        //                     Tag = filterText,
        //                     Background = Brushes.Transparent,
        //                     BorderBrush = Brushes.Transparent,
        //                     Name = "xButton"
        //                 },
        //                 new TextBlock
        //                 {
        //                     Text = filterText,
        //                     Margin = new Thickness(0, 0, 5, 0),
        //                     VerticalAlignment = VerticalAlignment.Center
        //                 }
        //             }
        //         }
        //     };

        //     // Hook up the button click event
        //     var button = (border.Child as StackPanel).Children[1] as Button;
        //     button.Click += RemoveFilterChip;

        //     // Add to the panel
        //     FilterChipPanel.Children.Add(border);
        // }

        // private void RemoveFilterChip(object sender, RoutedEventArgs e)
        // {
        //     if (sender is Button button && button.Parent is StackPanel stack && stack.Parent is Border border)
        //     {
        //         FilterChipPanel.Children.Remove(border);
                
        //         // Optional: do something with button.Tag (the filter identifier)
        //         string? removedFilter = button.Tag as string;
        //         Console.WriteLine($"Removed filter: {removedFilter}");
        //     }
        // }

        private void SortComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
            {
                filterSettings.SortBy = item.Tag!.ToString();
                RenewList();
                LoadNextPage();
            }
        }

        private void RenewList()
        {
            _moviesContainer?.Children.Clear();
            _currentOffset = 0;
        }

        #endregion
    }
}