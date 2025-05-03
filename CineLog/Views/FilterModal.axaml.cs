using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CineLog.Views.Helper;

namespace CineLog.Views
{
    public partial class FilterModal : Window
    {
        private Window? _owner;
        private Point _offset;
        private List<CheckBox>? _genreCheckBoxes;
        private List<CheckBox>? _companyCheckBoxes;

        public FilterModal()
        {
            throw new InvalidOperationException("This constructor is only for Avalonia XAML loader compatibility.");
        }

        public FilterModal(DatabaseHandler.FilterSettings filterSettings)
        {
            InitializeComponent();
            _genreCheckBoxes = AddCheckboxesToPanel(GenresPanel, DatabaseHandler.GetAllItems("genres_table"));
            _companyCheckBoxes = AddCheckboxesToPanel(CompaniesPanel, DatabaseHandler.GetAllItems("companies_table"));

            Opened += (s, e) =>
            {
                _owner = (Window)Owner!;
                if (_owner != null)
                {
                    _offset = new Point(
                        Position.X - _owner.Position.X,
                        Position.Y - _owner.Position.Y
                    );
                    
                    _owner.PositionChanged += Owner_PositionChanged;
                }
            };
            
            Closed += (s, e) =>
            {
                if (_owner != null) _owner.PositionChanged -= Owner_PositionChanged;
            };

            if (filterSettings != null) TickSettings(filterSettings);
        }

        private void TickSettings(DatabaseHandler.FilterSettings filterSettings)
        {
            foreach (var cb in _genreCheckBoxes!)
            {
                if (cb.Tag is string tag &&
                    filterSettings.Genre != null &&
                    filterSettings.Genre.Any(t => t.Item1 == tag))
                {
                    cb.IsChecked = true;
                }
            }

            foreach (var cb in _companyCheckBoxes!)
            {
                if (cb.Tag is string tag &&
                    filterSettings.Company != null &&
                    filterSettings.Company.Any(t => t.Item1 == tag))
                {
                    cb.IsChecked = true;
                }
            }

            MinRating.Text = filterSettings.MinRating?.ToString("0.0") ?? "0.0";
            MaxRating.Text = filterSettings.MaxRating?.ToString("0.0") ?? "10.0";

            YearStart.Text = filterSettings.YearStart.ToString();
            YearEnd.Text = filterSettings.YearEnd.ToString();

            foreach (var rb in TitleTypePanel.Children.OfType<RadioButton>())
            {
                var tag = rb.Tag?.ToString();
                if (tag == filterSettings.Type) rb.IsChecked = true;
                else rb.IsChecked = false;
            }

            SearchBox.Text = filterSettings.SearchTerm ?? "";
        }

        private void Owner_PositionChanged(object? sender, PixelPointEventArgs e)
        {
            Position = new PixelPoint(
                e.Point.X + (int)_offset.X,
                e.Point.Y + (int)_offset.Y
            );
        }

        private static List<CheckBox> AddCheckboxesToPanel(WrapPanel panel, IEnumerable<IdNameItem> items)
        {
            panel.Children.Clear();
            var checkBoxes = new List<CheckBox>();

            foreach (var item in items.OrderBy(i => i.Name))
            {
                var cb = new CheckBox
                {
                    Content = item.Name,
                    Tag = item.Id,
                    Margin = new Thickness(5),
                    Width = 220
                };
                checkBoxes.Add(cb);
                panel.Children.Add(cb);
            }

            return checkBoxes;
        }

        private static List<Tuple<string, string>> GetSelectedIds(List<CheckBox> checkBoxes)
        {
            return [.. checkBoxes
                .Where(cb => cb.IsChecked == true)
                .Select(cb => Tuple.Create(cb.Tag?.ToString() ?? "", cb.Content?.ToString() ?? ""))];
        }

        private void OnApplyClicked(object? sender, RoutedEventArgs e)
        {
            float minRating = 0.0f;
            float maxRating = 10.0f;

            // Read Rating values
            var minRatingBox = this.FindControl<TextBox>("MinRating");
            var maxRatingBox = this.FindControl<TextBox>("MaxRating");
            if (minRatingBox != null && float.TryParse(minRatingBox.Text, out float minRatingParsed)) minRating = minRatingParsed;
            if (maxRatingBox != null && float.TryParse(maxRatingBox.Text, out float maxratingParsed)) maxRating = maxratingParsed;

            // Read Year values
            int yearStart = 1874;
            int yearEnd = DateTime.Now.Year;

            var minYearBox = this.FindControl<TextBox>("YearStart");
            var maxYearBox = this.FindControl<TextBox>("YearEnd");
            if (minYearBox != null && int.TryParse(minYearBox.Text, out int minYearParsed)) yearStart = minYearParsed;
            if (maxYearBox != null && int.TryParse(maxYearBox.Text, out int maxYearParsed)) yearEnd = maxYearParsed;

            // Read Title Type (Movie or Series)
            string? selectedType = null;
            var typePanel = this.FindControl<WrapPanel>("TitleTypePanel");
            if (typePanel != null)
            {
                foreach (var child in typePanel.Children)
                {
                    if (child is RadioButton radioButton && radioButton.IsChecked == true && radioButton.Tag is string typeTag)
                    {
                        selectedType = typeTag;
                        break;
                    }
                }
            }

            var filterSettings = new DatabaseHandler.FilterSettings
            {
                MinRating = minRating,
                MaxRating = maxRating,
                Genre = GetSelectedIds(_genreCheckBoxes!),
                YearStart = yearStart,
                YearEnd = yearEnd,
                Company = GetSelectedIds(_companyCheckBoxes!),
                Type = selectedType,
                SearchTerm = SearchBox.Text
            };

            Close(filterSettings);
        }

        private void OnCloseClicked(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void OnClearClicked(object? sender, RoutedEventArgs e)
        {
            foreach (var cb in _genreCheckBoxes!) cb.IsChecked = false;

            foreach (var cb in _companyCheckBoxes!) cb.IsChecked = false;

            MinRating.Text = "0.0";
            MaxRating.Text = "10.0";

            YearStart.Text = "1874";
            YearEnd.Text = (DateTime.Now.Year + 1).ToString();

            foreach (var rb in TitleTypePanel.Children.OfType<RadioButton>()) rb.IsChecked = false;

            SearchBox.Text = "";
        }
    }
}