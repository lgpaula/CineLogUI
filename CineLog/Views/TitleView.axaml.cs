using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CineLog.Views.Helper;
using System;

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
        private StackPanel ?_titlePoster;

        public TitleView(string id)
        {
            Console.WriteLine($"TitleView constructor called with id: {id}");
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
            _titlePoster = this.FindControl<StackPanel>("TitlePoster")
                ?? throw new NullReferenceException("TitlePoster not found in XAML");
        }

        private async void LoadTitleInfo(string id)
        {
            Console.WriteLine($"LoadTitleInfo for id: {id}");
            var titleInfo = await DatabaseHandler.GetTitleInfo(id);
            Console.WriteLine($"LoadedTitleInfo");

            _titleTextBox!.Text = titleInfo.Title;

            _infoTextBlock!.Text = $"⭐ {titleInfo.Rating} • {titleInfo.YearStart}";
            if (titleInfo.YearEnd != null)
                _infoTextBlock.Text += $" - {titleInfo.YearEnd}";
            _infoTextBlock.Text += $" • {titleInfo.Runtime}";

            _descriptionBox!.Text = titleInfo.Plot;

            TryFill(_genreExpander, titleInfo.Genres);
            TryFill(_starsExpander, titleInfo.Stars);
            TryFill(_writersExpander, titleInfo.Writers);
            TryFill(_directorsExpander, titleInfo.Directors);
            TryFill(_creatorsExpander, titleInfo.Creators);
            TryFill(_companiesExpander, titleInfo.Companies);

            if (_titlePoster is not null && !string.IsNullOrWhiteSpace(titleInfo.PosterUrl))
            {
                Movie movie = new(titleInfo.PosterUrl);
                _titlePoster.Children.Add(movie.GetImageBorder());
            }
        }

        private static void TryFill(Expander? expander, string? data)
        {
            if (expander is not null && !string.IsNullOrWhiteSpace(data))
                FillExpander(expander, data);
        }

        private static void FillExpander(Expander expander, string items)
        {
            var panel = new StackPanel();
            var itemList = items.Split(',');

            foreach (var item in itemList)
            {
                panel.Children.Add(new TextBlock { Text = item.Trim(), FontSize = 12 });
            }

            expander.Content = panel;
        }
    }
}
