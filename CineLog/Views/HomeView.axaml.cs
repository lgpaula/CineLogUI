using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using CineLog.ViewModels;

namespace CineLog.Views
{
    public partial class HomeView : UserControl
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
        private readonly HttpClient _httpClient = new();

        public HomeView()
        {
            InitializeComponent();
            DatabaseHandler.CreateListsTable();
            _ = LoadCollection();
            _ = LoadLists();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task LoadCollection()
        {
            var movies = DatabaseHandler.GetMovies();
            Console.WriteLine($"Loaded {movies.Count} movies from collection");

            StackPanel? panel = this.FindControl<StackPanel>("CollectionContainer");

            foreach (var movie in movies)
            {
                Button movieButton = await movie.CreateMovieButton(_httpClient);
                panel?.Children.Add(movieButton);
            }
        }

        private async Task LoadLists()
        {
            var lists = DatabaseHandler.GetListsFromDatabase();

            foreach (var listName in lists)
            {
                try
                {
                    CreateListUI(listName);
                    var movies = DatabaseHandler.GetMovies(listName);
                    Console.WriteLine($"Loaded {movies.Count} movies");

                    StackPanel? panel = this.FindControl<StackPanel>(listName);

                    foreach (var movie in movies)
                    {
                        Button movieButton = await movie.CreateMovieButton(_httpClient);
                        panel?.Children.Add(movieButton);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading {listName}: {ex.Message}");
                }
            }
        }

        private void AddListToTable(object sender, RoutedEventArgs e)
        {
            var listName = DatabaseHandler.CreateNewList();
            CreateListUI(listName);
        }

        private void CreateListUI(string listName)
        {
            var listsContainer = this.FindControl<StackPanel>("ListsContainer");
            if (listsContainer is null) return;
    
            DockPanel dockPanel = new()
            {
                LastChildFill = true
            };

            TextBlock listTitle = new()
            {
                Text = listName,
                Foreground = Brushes.White,
                FontSize = 16
            };

            Button seeAllButton = new()
            {
                Content = "See all",
                FontSize = 16,
                Tag = listName,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.White
            };
            seeAllButton.Click += ViewChanger;
            seeAllButton.Tag = listName;

            DockPanel.SetDock(seeAllButton, Dock.Right);
            dockPanel.Children.Add(listTitle);
            dockPanel.Children.Add(seeAllButton);

            StackPanel listPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Name = listName
            };

            Border listContainer = new()
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = listPanel
                }
            };
            StackPanel wrapper = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5
            };

            wrapper.Children.Add(dockPanel);
            wrapper.Children.Add(listContainer);

            listsContainer.Children.Add(wrapper);
        }

        #region ViewModifier
            private void ViewChanger(object? sender, RoutedEventArgs e)
            {
                if (sender is Button button && button.Tag is string viewName)
                {
                    ViewModel?.HandleButtonClick(viewName);
                }
            }
        #endregion
    }
}