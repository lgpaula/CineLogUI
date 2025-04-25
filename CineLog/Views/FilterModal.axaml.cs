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

        private static List<string> GetSelectedIds(List<CheckBox> checkBoxes)
        {
            return [.. checkBoxes
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Tag)
                .OfType<string>()];
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
            var filterSettings = new DatabaseHandler.FilterSettings
            {
                MinRating = minRating,
                MaxRating = maxRating,
                Genre = GetSelectedIds(_genreCheckBoxes!),
                YearStart = yearStart,
                YearEnd = yearEnd,
                Company = GetSelectedIds(_companyCheckBoxes!),
                Type = selectedTypes.Count > 0 ? string.Join(",", selectedTypes) : null
            };

            Close(filterSettings); // return filters to CollectionView
        }

        private void OnCloseClicked(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}