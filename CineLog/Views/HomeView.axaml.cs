using System;
using System.Net.Http;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using CineLog.ViewModels;
using System.Collections.Generic;

namespace CineLog.Views
{
    public partial class HomeView : UserControl
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
        private readonly HttpClient _httpClient = new();
        private readonly Dictionary<string, StackPanel> _listPanels = new();

        public HomeView()
        {
            InitializeComponent();
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
            if (!_listPanels.TryGetValue(containerName, out StackPanel? panel))
            {
                panel = CreateListPanel(containerName);
                _listPanels[containerName] = panel;
            }

            panel.Children.Clear();

            var movies = DatabaseHandler.GetMovies(listName);

            foreach (var movie in movies)
            {
                Button movieButton = movie.CreateMovieButton(_httpClient);
                panel?.Children.Add(movieButton);
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
            seeAllButton.Tag = listName;

            DockPanel.SetDock(seeAllButton, Dock.Right);
            dockPanel.Children.Add(listTitle);
            dockPanel.Children.Add(seeAllButton);

            StackPanel listPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Name = listName
            };

            listsContainer?.Children.Add(dockPanel);
            listsContainer?.Children.Add(listPanel);

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

        #endregion
    }
}