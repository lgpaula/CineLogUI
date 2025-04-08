using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls;
using Newtonsoft.Json;

namespace CineLog.Views
{
    public partial class ScraperView : UserControl
    {
        private readonly Button _scrapeButton = null!;
        private readonly List<CheckBox> _genreCheckBoxes = null!;
        private readonly List<CheckBox> _companyCheckBoxes = null!;
        private readonly List<CheckBox> _typeCheckBoxes = null!;
        private readonly List<CheckBox> _keywordCheckBoxes = null!;
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
            _companyCheckBoxes = GetCheckBoxes("CompanyPanel");
            _typeCheckBoxes = GetCheckBoxes("TypePanel");
            _keywordCheckBoxes = GetCheckBoxes("KeywordPanel");

            _scrapeButton.Click += OnScrapeButtonClick;
        }

        private void OnScrapeButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var criteria = new ScraperCriteria
            {
                Genres = GetSelectedCheckBoxes(_genreCheckBoxes),
                Companies = GetSelectedCheckBoxes(_companyCheckBoxes),
                Types = GetSelectedCheckBoxes(_typeCheckBoxes),
                Keywords = GetSelectedCheckBoxes(_keywordCheckBoxes),
                YearFrom = TryParseInt(_yearFrom.Text),
                YearTo = TryParseInt(_yearTo.Text),
                RatingFrom = TryParseFloat(_ratingFrom.Text),
                RatingTo = TryParseFloat(_ratingTo.Text)
            };

            _ = StartScraping(criteria);
        }

        private static async Task StartScraping(ScraperCriteria criteria)
        {
            string stringCriteria = ConvertCriteria(criteria);
            string response = await Helper.ServerHandler.ScrapeMultipleTitles(stringCriteria);
            Console.WriteLine("Response: " + response);
        }

        private List<CheckBox> GetCheckBoxes(string parentName)
        {
            if (this.FindControl<WrapPanel>(parentName) is WrapPanel panel)
                return [.. panel.Children.OfType<CheckBox>()];
            return [];
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
                { "keywords", criteria.Keywords },
                { "yearFrom", criteria.YearFrom },
                { "yearTo", criteria.YearTo },
                { "ratingFrom", criteria.RatingFrom },
                { "ratingTo", criteria.RatingTo }
            };

            var stringCriteria = JsonConvert.SerializeObject(dict);
            Console.WriteLine("stringCriteria: " + stringCriteria);

            return stringCriteria;
        }
    }

    public struct ScraperCriteria
    {
        public List<string> Genres { get; set; }
        public List<string> Companies { get; set; }
        public List<string> Types { get; set; }
        public List<string> Keywords { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public float? RatingFrom { get; set; }
        public float? RatingTo { get; set; }
    }
}
