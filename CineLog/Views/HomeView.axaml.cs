using System;
using System.Data.SQLite;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using CineLog.ViewModels;

namespace CineLog.Views
{
    public partial class HomeView : UserControl
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
        private StackPanel? _moviesContainer;

        public HomeView()
        {
            InitializeComponent();
            LoadMovies();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            Console.WriteLine("home");
            _moviesContainer = this.FindControl<StackPanel>("MoviesContainer");
        }

        private void LoadMovies()
        {
            List<string> movies = GetMoviesFromDatabase();

            foreach (var movie in movies)
            {
                Border movieBox = new()
                {
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    Width = 150, // Adjust size as needed
                    Height = 200, // Adjust size as needed
                    Child = new TextBlock
                    {
                        Text = movie,
                        Foreground = Brushes.White,
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    }
                };

                _moviesContainer?.Children.Add(movieBox);
            }
        }

        private List<string> GetMoviesFromDatabase()
        {
            List<string> movies = [];

            string dbPath = "example.db"; // Ensure the path is correct
            string connectionString = $"Data Source={dbPath};Version=3;";

            using (SQLiteConnection conn = new(connectionString))
            {
                conn.Open();
                string sql = "SELECT title_name FROM titles"; // Adjust table and column name
                using SQLiteCommand cmd = new(sql, conn);
                using SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    movies.Add(reader.GetString(0));
                }
            }

            return movies;
        }

        private void ClickHandler(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string viewName)
            {
                ViewModel.HandleButtonClick(viewName);
            }
        }
    }
}
