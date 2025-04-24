using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls;
using Newtonsoft.Json;
using CineLog.Views.Helper;

namespace CineLog.Views
{
    public partial class ScraperView : UserControl
    {
        private readonly Button _scrapeButton = null!;
        private readonly List<CheckBox> _genreCheckBoxes = null!;
        private readonly List<CheckBox> _companyCheckBoxes = null!;
        private readonly List<CheckBox> _typeCheckBoxes = null!;
        private readonly TextBox _yearFrom = null!;
        private readonly TextBox _yearTo = null!;
        private readonly TextBox _ratingFrom = null!;
        private readonly TextBox _ratingTo = null!;

        public ScraperView()
        {
            InitializeComponent();

            _scrapeButton = this.FindControl<Button>("scrapeButton") ?? throw new InvalidOperationException("scrapeButton not found");
            _yearFrom = this.FindControl<TextBox>("yearFrom") ?? throw new InvalidOperationException("yearFrom not found");
            _yearTo = this.FindControl<TextBox>("yearTo") ?? throw new InvalidOperationException("yearTo not found");
            _ratingFrom = this.FindControl<TextBox>("ratingFrom") ?? throw new InvalidOperationException("ratingFrom not found");
            _ratingTo = this.FindControl<TextBox>("ratingTo") ?? throw new InvalidOperationException("ratingTo not found");

            _genreCheckBoxes = GetCheckBoxes("GenrePanel");
            _typeCheckBoxes = GetCheckBoxes("TypePanel");

            _scrapeButton.Click += OnScrapeButtonClick;

            _companyCheckBoxes = AddCheckboxesToPanel(CompanyPanel, DatabaseHandler.GetAllItems("companies_table"));
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
                    Margin = new Avalonia.Thickness(5),
                    Width = 300
                };
                checkBoxes.Add(cb);
                panel.Children.Add(cb);
            }

            return checkBoxes;
        }

        private void OnScrapeButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var criteria = new ScraperCriteria
            {
                Genres = GetSelectedCheckBoxes(_genreCheckBoxes),
                Companies = GetSelectedIds(_companyCheckBoxes),
                Types = GetSelectedCheckBoxes(_typeCheckBoxes),
                YearFrom = TryParseInt(_yearFrom.Text),
                YearTo = TryParseInt(_yearTo.Text),
                RatingFrom = TryParseFloat(_ratingFrom.Text),
                RatingTo = TryParseFloat(_ratingTo.Text)
            };

            _ = StartScraping(criteria);
        }

        private static List<string> GetSelectedIds(List<CheckBox> checkBoxes)
        {
            return [.. checkBoxes
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Tag)
                .OfType<string>()];
        }

        private List<CheckBox> GetCheckBoxes(string parentName)
        {
            if (this.FindControl<WrapPanel>(parentName) is WrapPanel panel)
                return [.. panel.Children.OfType<CheckBox>()];
            return [];
        }

        private static async Task StartScraping(ScraperCriteria criteria)
        {
            string stringCriteria = ConvertCriteria(criteria);
            await Helper.ServerHandler.ScrapeMultipleTitles(stringCriteria);
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
