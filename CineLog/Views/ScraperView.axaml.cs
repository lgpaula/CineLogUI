using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls;
using Newtonsoft.Json;
using CineLog.Views.Helper;
using Avalonia;
using System;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace CineLog.Views
{
    public partial class ScraperView : UserControl
    {
        private List<CheckBox>? _genreCheckBoxes;
        private List<CheckBox>? _typeCheckBoxes;

        public ScraperView()
        {
            InitializeComponent();
            AttachedToVisualTree += OnLoaded;

            IncreaseButton.Click += (_, _) => ChangeQuantity(50);
            DecreaseButton.Click += (_, _) => ChangeQuantity(-50);
        }

        private void OnLoaded(object? sender, VisualTreeAttachmentEventArgs e)
        {
            var genresPanel = this.FindControl<WrapPanel>("GenresPanel");
            var typePanel = this.FindControl<WrapPanel>("TitleTypePanel");

            _genreCheckBoxes = GetCheckBoxes(genresPanel!);
            _typeCheckBoxes = GetCheckBoxes(typePanel!);
        }

        private void OnScrapeButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var types = GetSelectedCheckBoxes(_typeCheckBoxes!);
            if (types.Count == 0)
            {
                types = [.. _typeCheckBoxes!.Select(cb => cb.Content?.ToString() ?? "")];
            }
            var criteria = new ScraperCriteria
            {
                Genres = GetSelectedCheckBoxes(_genreCheckBoxes!),
                Types = types,
                YearFrom = TryParseInt(YearStart.Text),
                YearTo = TryParseInt(YearEnd.Text),
                RatingFrom = TryParseFloat(MinRating.Text),
                RatingTo = TryParseFloat(MaxRating.Text)
            };

            _ = StartScraping(criteria, TryParseInt(Quantity.Text));

            EventAggregator.Instance.Publish(new NotificationEvent { Message = $"✅ Scraping started. {Time.Text}. Please wait." });
            scrapeButton.IsEnabled = false;
        }

        private void OnAddExtraFilterClick(object? sender, RoutedEventArgs e)
        {
            var parentPanel = this.FindControl<StackPanel>("ExtraFilterPanel");
            var rowPanel = new StackPanel 
            { 
                Orientation = Orientation.Vertical, 
                Margin = new Thickness(0, 0, 0, 10) 
            };

            var searchPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };
            var comboBox = new ComboBox 
            {
                Width = 120,
                ItemsSource = new List<string> { "Company", "People", "Keyword" } // struct?
            };
            var textBox = new TextBox
            {
                Width = 200,
                Watermark = "Type to search...",
                IsEnabled = false
            };
            var deleteButton = new Button
            {
                Content = "X"
            };

            comboBox.SelectionChanged += (_, _) =>
            {
                textBox.IsEnabled = comboBox.SelectedItem != null;
            };

            searchPanel.Children.Add(comboBox);
            searchPanel.Children.Add(textBox);
            searchPanel.Children.Add(deleteButton);

            // suggestions
            var suggestionsPanel = new ItemsControl
            {
                Name = "SuggestionsPanel",
                ItemTemplate = new FuncDataTemplate<IdNameItem>((item, _) =>
                {
                    var textBlock = new TextBlock
                    {
                        Margin = new Thickness(5)
                    };
                    textBlock.Bind(TextBlock.TextProperty, new Binding("Name"));
                    textBlock.PointerPressed += Suggestion_Clicked!;
                    return textBlock;
                })
            };
            var suggestions = new ObservableCollection<IdNameItem>();
            suggestionsPanel.ItemsSource = suggestions;

            // handle input + suggestions
            textBox.GetObservable(TextBox.TextProperty).Subscribe(async text =>
            {
                var selected = comboBox.SelectedItem?.ToString();
                if (!string.IsNullOrWhiteSpace(selected) && text?.Length >= 3)
                {
                    var results = await DatabaseHandler.QueryDatabaseAsync(selected, text);
                    suggestions.Clear();
                    if (results.Count != 0)
                    {
                        foreach (var (id, name) in results)
                        {
                            suggestions.Add(new IdNameItem { Id = id, Name = name });
                        }
                    }
                    else
                    {
                        suggestions.Add(new IdNameItem { Id = "", Name = $"No results for \"{text}\"" });
                    }
                }
            });

            deleteButton.Click += (_, _) =>
            {
                parentPanel!.Children.Remove(rowPanel);
            };

            rowPanel.Children.Add(searchPanel);
            rowPanel.Children.Add(suggestionsPanel);
            parentPanel!.Children.Add(rowPanel);
        }

        private void Suggestion_Clicked(object sender, PointerPressedEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is IdNameItem item)
            {
                tb.Text = item.Name;
                tb.Tag = item.Id;
            }
        }

        private static List<string> GetSelectedIds(IEnumerable<Control> controls)
        {
            return [.. controls
                .Where(c => (c as CheckBox)?.IsChecked == true)
                .Select(c => c.Tag)
                .OfType<string>()];
        }

        private static List<CheckBox> GetCheckBoxes(WrapPanel panel)
        {
            return [.. panel.Children.OfType<CheckBox>()];
        }

        private async Task StartScraping(ScraperCriteria criteria, int? quantity)
        {
            string stringCriteria = ConvertCriteria(criteria);
            await ServerHandler.ScrapeMultipleTitles(stringCriteria, quantity);

            scrapeButton.IsEnabled = true;
        }

        private static List<string> GetSelectedCheckBoxes(List<CheckBox> checkBoxes)
        {
            return [.. checkBoxes.Where(cb => cb.IsChecked == true).Select(cb => cb.Content?.ToString() ?? "")];
        }

        private static int? TryParseInt(string? text)
        {
            return int.TryParse(text, out var result) ? result : null;
        }

        private static float? TryParseFloat(string? text)
        {
            return float.TryParse(text, out var result) ? result : null;
        }

        private static string ConvertCriteria(ScraperCriteria criteria)
        {
            var dict = new Dictionary<string, object?>
            {
                { "genres", criteria.Genres },
                { "companies", criteria.Companies },
                { "types", criteria.Types },
                { "yearFrom", criteria.YearFrom },
                { "yearTo", criteria.YearTo },
                { "ratingFrom", criteria.RatingFrom },
                { "ratingTo", criteria.RatingTo }
            };

            var stringCriteria = JsonConvert.SerializeObject(dict);

            return stringCriteria;
        }

        private void ChangeQuantity(int delta)
        {
            if (int.TryParse(Quantity.Text, out int currentQuantity))
            {
                currentQuantity += delta;
                currentQuantity = Math.Clamp(currentQuantity, 50, 1000);

                Quantity.Text = currentQuantity.ToString();

                int timeSeconds = currentQuantity / 50 * 10;
                Time.Text = $"Estimated time: {timeSeconds} seconds";
            }
        }
    }

    public struct ScraperCriteria
    {
        public List<string> Genres { get; set; }
        public List<string> Companies { get; set; }
        public List<string> Types { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public float? RatingFrom { get; set; }
        public float? RatingTo { get; set; }
    }

    public class IdNameItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
