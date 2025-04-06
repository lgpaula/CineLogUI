using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace CineLog.Views
{
    public partial class TitleView : UserControl
    {
        private TextBlock ?_titleTextBox;
        private TextBlock ?_infoTextBlock;
        private TextBlock ?_descriptionBox;
        private Expander ?_genreExpander;
        private Expander ?_starsExpander;
        private Expander ?_writersExpander;
        private Expander ?_directorsExpander;
        private Expander ?_creatorsExpander;
        private Expander ?_companiesExpander;

        public TitleView(string id)
        {
            InitializeComponent();
            LoadTitleInfo(id);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _titleTextBox = this.FindControl<TextBlock>("TitleTextBox")
                ?? throw new NullReferenceException("TitleTextBox not found in XAML");
            _infoTextBlock = this.FindControl<TextBlock>("InfoTextBlock")
                ?? throw new NullReferenceException("InfoTextBlock not found in XAML");
            _descriptionBox = this.FindControl<TextBlock>("DescriptionBox")
                ?? throw new NullReferenceException("DescriptionBox not found in XAML");
            _genreExpander = this.FindControl<Expander>("GenreExpander")
                ?? throw new NullReferenceException("GenreExpander not found in XAML");
            _starsExpander = this.FindControl<Expander>("StarsExpander")
                ?? throw new NullReferenceException("StarsExpander not found in XAML");
            _writersExpander = this.FindControl<Expander>("WritersExpander")
                ?? throw new NullReferenceException("WritersExpander not found in XAML");
            _directorsExpander = this.FindControl<Expander>("DirectorsExpander")
                ?? throw new NullReferenceException("DirectorsExpander not found in XAML");
            _creatorsExpander = this.FindControl<Expander>("CreatorsExpander")
                ?? throw new NullReferenceException("CreatorsExpander not found in XAML");
            _companiesExpander = this.FindControl<Expander>("CompaniesExpander")
                ?? throw new NullReferenceException("CompaniesExpander not found in XAML");
        }

        private void LoadTitleInfo(string id)
        {
            // Simulated loading - replace with actual logic
            var titleInfo = new
            {
                Title = "The Matrix",
                Year = 1999,
                Runtime = "2h 16m",
                Description = "A computer hacker learns about the true nature of reality.",
                Genres = new List<string> { "Action", "Sci-Fi" },
                Stars = new List<string> { "Keanu Reeves", "Laurence Fishburne" },
                Writers = new List<string> { "Lana Wachowski", "Lilly Wachowski" },
                Directors = new List<string> { "The Wachowskis" },
                Creators = new List<string> { },
                Companies = new List<string> { "Warner Bros." }
            };

            if (_titleTextBox != null)
            {
                _titleTextBox.Text = titleInfo.Title;
            }
            if (_infoTextBlock != null)
            {
                _infoTextBlock.Text = $"⭐ {titleInfo.Year} • {titleInfo.Runtime}";
            }
            if (_descriptionBox != null)
            {
                _descriptionBox.Text = titleInfo.Description;
            }

            FillExpander(_genreExpander, titleInfo.Genres);
            FillExpander(_starsExpander, titleInfo.Stars);
            FillExpander(_writersExpander, titleInfo.Writers);
            FillExpander(_directorsExpander, titleInfo.Directors);
            FillExpander(_creatorsExpander, titleInfo.Creators);
            FillExpander(_companiesExpander, titleInfo.Companies);
        }

        private static void FillExpander(Expander expander, List<string> items)
        {
            var panel = new StackPanel();
            foreach (var item in items)
            {
                panel.Children.Add(new TextBlock { Text = item, FontSize = 16 });
            }

            expander.Content = panel;
        }
    }
}
