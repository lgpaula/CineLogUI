using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace CineLog.Views
{
    public partial class CollectionView : UserControl
    {
        public string viewName = string.Empty;
        private int _currentOffset = 0;
        private const int count = 50;

        public CollectionView(string viewName)
        {
            this.viewName = viewName;
            InitializeComponent();
        }

        public CollectionView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var _moviesContainer = this.FindControl<WrapPanel>("CollectionWrapPanel")
                        ?? throw new NullReferenceException("WrapPanel not found in XAML");
            var _scrollViewer = this.FindControl<ScrollViewer>("CollectionScrollViewer") 
                        ?? throw new NullReferenceException("ScrollViewer not found in XAML");

            LoadNextPage(_moviesContainer);

            _scrollViewer.ScrollChanged += (sender, e) => OnScrollChanged(_scrollViewer, _moviesContainer);
        }

        private void LoadNextPage(WrapPanel wrapPanel)
        {
            var movies = DatabaseHandler.GetMovies(viewName, count, _currentOffset);

            foreach (var movie in movies)
            {
                var movieButton = movie.CreateMovieButton();
                wrapPanel.Children.Add(movieButton);
            }

            Console.WriteLine($"Loaded {count} more movies (starting from offset {_currentOffset})");

            _currentOffset += count;
        }

        private void OnScrollChanged(ScrollViewer scrollViewer, WrapPanel wrapPanel)
        {
            Console.WriteLine($"Offset.Y: {scrollViewer.Offset.Y}, window.Height: {scrollViewer.Viewport.Height}, window.Width: {scrollViewer.Extent.Height}");
            if (scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height - 100)
            {
                // Console.WriteLine("Loading more items...");
                LoadNextPage(wrapPanel);
            }
        }

    }
}