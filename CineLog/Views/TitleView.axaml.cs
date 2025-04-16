using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
        private ScrollViewer ?_genreViewer;
        private ScrollViewer ?_starsViewer;
        private ScrollViewer ?_writersViewer;
        private ScrollViewer ?_directorsViewer;
        private ScrollViewer ?_creatorsViewer;
        private ScrollViewer ?_companiesViewer;
        private StackPanel ?_titlePoster;
        private readonly Movie _currMovie;

        public TitleView(string id)
        {
            InitializeComponent();
            _currMovie = new(id);
            LoadTitleInfo();
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
            _genreViewer = this.FindControl<ScrollViewer>("GenreScrollViewer")
                ?? throw new NullReferenceException("GenreExpander not found in XAML");
            _starsViewer = this.FindControl<ScrollViewer>("StarsScrollViewer")
                ?? throw new NullReferenceException("StarsExpander not found in XAML");
            _writersViewer = this.FindControl<ScrollViewer>("WritersScrollViewer")
                ?? throw new NullReferenceException("WritersExpander not found in XAML");
            _directorsViewer = this.FindControl<ScrollViewer>("DirectorsScrollViewer")
                ?? throw new NullReferenceException("DirectorsExpander not found in XAML");
            _creatorsViewer = this.FindControl<ScrollViewer>("CreatorsScrollViewer")
                ?? throw new NullReferenceException("CreatorsExpander not found in XAML");
            _companiesViewer = this.FindControl<ScrollViewer>("CompaniesScrollViewer")
                ?? throw new NullReferenceException("CompaniesExpander not found in XAML");
            _titlePoster = this.FindControl<StackPanel>("TitlePoster")
                ?? throw new NullReferenceException("TitlePoster not found in XAML");
        }

        private async void LoadTitleInfo()
        {
            var titleInfo = await DatabaseHandler.GetTitleInfo(_currMovie.Id);

            _titleTextBox!.Text = titleInfo.Title_name;

            _infoTextBlock!.Text = $" {titleInfo.Rating} • {titleInfo.Year_start}";
            if (titleInfo.Year_end != null)
                _infoTextBlock.Text += $" - {titleInfo.Year_end}";

            _infoTextBlock.Text += $" • {titleInfo.Runtime}";
            
            if (!string.IsNullOrEmpty(titleInfo.Season_count))
                _infoTextBlock.Text += $" • {titleInfo.Season_count} Seasons";

            _descriptionBox!.Text = titleInfo.Plot;

            TryFill(_genreViewer, titleInfo.Genres);
            TryFill(_starsViewer, titleInfo.Stars);
            TryFill(_writersViewer, titleInfo.Writers);
            TryFill(_directorsViewer, titleInfo.Directors);
            TryFill(_creatorsViewer, titleInfo.Creators);
            TryFill(_companiesViewer, titleInfo.Companies);

            if (_titlePoster is not null && !string.IsNullOrWhiteSpace(titleInfo.Poster_url))
            {
                _titlePoster.Children.Add(_currMovie.GetImageBorder());
            }
        }

        private static void TryFill(ScrollViewer? wrap, string? data)
        {
            if (wrap is not null && !string.IsNullOrWhiteSpace(data))
                FillExpander(wrap, data);
        }

        private static void FillExpander(ScrollViewer wrap, string items)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            var itemList = items.Split(',');

            foreach (var item in itemList)
            {
                panel.Children.Add(new Button 
                {
                    Content = item.Trim(),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 5, 0),
                });
            }

            wrap.Content = panel;
        }

        private void AddToCalendar(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string title_id)
            {
                var scheduleList = DatabaseHandler.GetSchedule(title_id);
                var title_button = _currMovie.CreateMovieButton();
                CalendarView.AddMovieToCalendar(scheduleList, title_button);
            }
        }
    }
}
