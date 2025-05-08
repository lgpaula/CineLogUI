using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls;
using Newtonsoft.Json;
using CineLog.Views.Helper;
using Avalonia;
using System;

namespace CineLog.Views
{
    public partial class ScraperView : UserControl
    {
        private List<CheckBox>? _genreCheckBoxes;
        private List<CheckBox>? _companyCheckBoxes;
        private List<CheckBox>? _typeCheckBoxes;
        private List<IdNameItem>? _allCompanyItems;
        private int _loadedCompanyCount = 0;
        private const int CompanyBatchSize = 50;

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
            var companiesPanel = this.FindControl<WrapPanel>("CompaniesPanel");

            _genreCheckBoxes = GetCheckBoxes(genresPanel!);
            _typeCheckBoxes = GetCheckBoxes(typePanel!);
            _allCompanyItems = [.. DatabaseHandler.GetAllItems("companies_table").OrderBy(i => i.Name)];
            AddCompanyCheckboxesChunk();
            CompanyScrollViewer.ScrollChanged += OnCompanyScrollChanged!;
        }

        private void AddCompanyCheckboxesChunk()
        {
            int remaining = _allCompanyItems!.Count - _loadedCompanyCount;
            if (remaining <= 0) return;

            int toLoad = Math.Min(CompanyBatchSize, remaining);
            var nextItems = _allCompanyItems
                .Skip(_loadedCompanyCount)
                .Take(toLoad);

            foreach (var item in nextItems)
            {
                var cb = new CheckBox
                {
                    Content = item.Name,
                    Tag = item.Id,
                    Margin = new Thickness(5),
                    Width = 300
                };
                CompaniesPanel.Children.Add(cb);
            }

            _loadedCompanyCount += toLoad;
        }

        private void OnCompanyScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.Source is not ScrollViewer scroll) return;

            double offset = scroll.Offset.Y;
            double max = scroll.Extent.Height - scroll.Viewport.Height;

            if (max - offset < 200)
            {
                AddCompanyCheckboxesChunk();
            }
        }

        private void OnScrapeButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var types = GetSelectedCheckBoxes(_typeCheckBoxes!);
            if (types.Count == 0)
            {
                types = [.. _typeCheckBoxes!.Select(cb => cb.Content?.ToString() ?? "")];
            }
            _companyCheckBoxes = GetCheckBoxes(CompaniesPanel!);
            var criteria = new ScraperCriteria
            {
                Genres = GetSelectedCheckBoxes(_genreCheckBoxes!),
                Companies = GetSelectedIds(_companyCheckBoxes!),
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

        private static List<string> GetSelectedIds(List<CheckBox> checkBoxes)
        {
            return [.. checkBoxes
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Tag)
                .OfType<string>()];
        }

        private static List<CheckBox> GetCheckBoxes(WrapPanel panel)
        {
            return [.. panel.Children.OfType<CheckBox>()];
        }

        private static async Task StartScraping(ScraperCriteria criteria, int? quantity)
        {
            string stringCriteria = ConvertCriteria(criteria);
            await ServerHandler.ScrapeMultipleTitles(stringCriteria, quantity);
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
