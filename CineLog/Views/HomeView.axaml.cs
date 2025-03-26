using System;
using System.Net.Http;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using CineLog.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace CineLog.Views
{
    public partial class HomeView : UserControl
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
        private readonly HttpClient _httpClient = new();
        private readonly List<StackPanel> _listPanels = [];

        public HomeView()
        {
            InitializeComponent();
            var preLoadedPanel = this.FindControl<StackPanel>("CollectionContainer");
            if (preLoadedPanel != null)
            {
                _listPanels.Add(preLoadedPanel);
            }
            EventAggregator.Instance.Subscribe("ListUpdated", LoadListUI);
            DatabaseHandler.CreateListsTable();
            LoadMoviesAndLists();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadMoviesAndLists()
        {
            LoadListUI("CollectionContainer", null);

            var lists = DatabaseHandler.GetListsFromDatabase();
            foreach (var listName in lists)
            {
                try
                {
                    LoadListUI(listName, listName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading {listName}: {ex.Message}");
                }
            }
        }

        private void LoadListUI(string containerName, string? listName)
        {
            StackPanel? panel = _listPanels.FirstOrDefault(p => p.Name == containerName);
            panel ??= CreateListPanel(containerName);

            var moviesInDatabase = DatabaseHandler.GetMovies(listName);

            var movieButtonsInUI = new Dictionary<string, Button>();
            foreach (var child in panel.Children)
            {
                if (child is Button button && button.Tag is string movieId)
                    movieButtonsInUI[movieId] = button;
            }

            var movieIdsInDatabase = new HashSet<string>(moviesInDatabase.ConvertAll(m => m.Id));
            var movieIdsInUI = new HashSet<string>(movieButtonsInUI.Keys);

            foreach (var movie in moviesInDatabase)
            {
                if (!movieIdsInUI.Contains(movie.Id))
                {
                    Button movieButton = movie.CreateMovieButton(_httpClient);
                    movieButton.Tag = movie.Id;
                    panel.Children.Add(movieButton);
                }
            }

            foreach (var movieId in movieIdsInUI)
            {
                if (!movieIdsInDatabase.Contains(movieId))
                {
                    panel.Children.Remove(movieButtonsInUI[movieId]);
                }
            }
        }

        private StackPanel CreateListPanel(string listName)
        {
            var listsContainer = this.FindControl<StackPanel>("ListsContainer");
            if (listsContainer is null) return new StackPanel();

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

            Button deleteListButton = new()
            {
                Content = "Delete",
                FontSize = 16,
                Tag = listName,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.White
            };
            deleteListButton.Click += DeleteList;

            DockPanel.SetDock(seeAllButton, Dock.Right);
            DockPanel.SetDock(deleteListButton, Dock.Right);
            dockPanel.Children.Add(listTitle);
            dockPanel.Children.Add(seeAllButton);
            dockPanel.Children.Add(deleteListButton);

            StackPanel listPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Name = listName
            };

            listsContainer?.Children.Add(dockPanel);
            listsContainer?.Children.Add(listPanel);

            _listPanels.Add(listPanel);
            return listPanel;
        }

        #region Buttons From AXAML
            private void ViewChanger(object? sender, RoutedEventArgs e)
            {
                if (sender is Button button && button.Tag is string viewName)
                {
                    ViewModel?.HandleButtonClick(viewName);
                }
            }

            private void AddListToTable(object sender, RoutedEventArgs e)
            {
                var listName = DatabaseHandler.CreateNewList();
                CreateListPanel(listName);
            }

            private void DeleteList(object? sender, RoutedEventArgs e)
            {
                if (sender is Button button && button.Tag is string listName)
                {
                    DatabaseHandler.DeleteList(listName);
                    var panel = _listPanels.FirstOrDefault(p => p.Name == listName);
                    Console.WriteLine(panel == null);
                    if (panel != null)
                    {
                        if (panel.Parent is DockPanel dockPanel)
                        {
                            dockPanel.Children.Remove(panel);
                            dockPanel.Children.Remove(dockPanel.Children.First(c => c is TextBlock block && block.Text == listName));
                        }
                        _listPanels.Remove(panel);
                    }
                }

                LoadMoviesAndLists();
            }

        #endregion
    }
}