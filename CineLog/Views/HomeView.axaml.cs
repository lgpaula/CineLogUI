using System;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using CineLog.ViewModels;
using CineLog.Views.Helper;
using Avalonia.Controls.Primitives;
using Avalonia;
using Avalonia.Input;

namespace CineLog.Views
{
    public partial class HomeView : UserControl
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
        private readonly List<StackPanel> _panelsList = [];

        public HomeView()
        {
            InitializeComponent();
            EventAggregator.Instance.Subscribe("ListUpdated", LoadListUI);

            DatabaseHandler.CreateListsTable();
            DatabaseHandler.CreateCalendarTable();
            LoadMoviesAndLists();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadMoviesAndLists()
        {
            _panelsList.Add(this.FindControl<StackPanel>("CollectionContainer")!);

            LoadListUI("CollectionContainer", null);

            var lists = DatabaseHandler.GetListsFromDatabase();
            foreach (var (list_name, list_uuid) in lists)
            {
                LoadListUI(list_name, list_uuid);
            }
        }

        private void LoadListUI(string listName, string? listId)
        {
            StackPanel? panel = _panelsList.FirstOrDefault(p => p.Name == (listId ?? listName));
            panel ??= CreateListPanel(listName, listId ?? listName);

            var sqlQuery = new DatabaseHandler.SQLQuerier {
                List_uuid = listId,
                Limit = 20
            };

            var moviesInDatabase = DatabaseHandler.GetMovies(sqlQuery);

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
                    Button movieButton = movie.CreateMovieButton();
                    movieButton.Tag = movie.Id;
                    movieButton.Cursor = new Cursor(StandardCursorType.Arrow);
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

        private StackPanel CreateListPanel(string listName, string listId)
        {
            var listsContainer = this.FindControl<StackPanel>("CustomListsContainer")!;

            DockPanel dockPanel = new()
            {
                LastChildFill = false
            };

            TextBox listTitle = new()
            {
                Text = listName,
                Foreground = Brushes.White,
                FontSize = 16,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Margin = new Thickness(0, 0, 5, 0)
            };

            listTitle.LostFocus += (s, e) =>
            {
                if (listTitle.Text != listName)
                {
                    DatabaseHandler.UpdateListName(listName, listTitle.Text);
                    listName = listTitle.Text;
                }
            };

            StackPanel listPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Name = listId
            };

            ScrollViewer scrollViewer = new()
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = listPanel
            };

            Border listBorder = new()
            {
                Child = scrollViewer
            };

            Button seeAllButton = new()
            {
                Content = "See all",
                FontSize = 16,
                Tag = listId,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.White
            };
            seeAllButton.Click += ViewChanger;

            Button deleteListButton = new()
            {
                Content = "Delete",
                FontSize = 16,
                Tag = listId,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.White,
                Margin = new Thickness(0, 0, 5, 0)
            };
            deleteListButton.Click += DeleteList;

            Button showListButton = new()
            {
                Content = "+",
                FontSize = 10,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.White,
                IsVisible = false
            };

            Button hideListButton = new()
            {
                Content = "-",
                FontSize = 10,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.White
            };

            var panelGroup = new ListPanelGroup
            {
                ContentPanel = listPanel,
                ShowButton = showListButton,
                HideButton = hideListButton,
                DeleteButton = deleteListButton,
                SeeAllButton = seeAllButton
            };

            showListButton.Tag = panelGroup;
            hideListButton.Tag = panelGroup;

            showListButton.Click += ShowPanel;
            hideListButton.Click += HidePanel;

            DockPanel.SetDock(listTitle, Dock.Left);
            DockPanel.SetDock(deleteListButton, Dock.Right);
            DockPanel.SetDock(seeAllButton, Dock.Right);
            DockPanel.SetDock(hideListButton, Dock.Left);
            DockPanel.SetDock(showListButton, Dock.Left);

            dockPanel.Children.Add(listTitle);
            dockPanel.Children.Add(seeAllButton);
            dockPanel.Children.Add(deleteListButton);
            dockPanel.Children.Add(hideListButton);
            dockPanel.Children.Add(showListButton);

            listsContainer.Children.Add(dockPanel);
            listsContainer.Children.Add(listBorder);

            _panelsList.Add(listPanel);
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
                var list = DatabaseHandler.CreateNewList();
                CreateListPanel(list[0], list[1]);
            }

            private void DeleteList(object? sender, RoutedEventArgs e)
            {
                if (sender is Button button && button.Tag is string listName)
                {
                    DatabaseHandler.DeleteList(listName);

                    var container = this.FindControl<StackPanel>("CustomListsContainer");
                    container?.Children.Clear();
                    _panelsList.Clear();

                    LoadMoviesAndLists();
                }
            }

            private void HidePanel(object? sender, RoutedEventArgs e)
            {
                if (sender is Button button && button.Tag is ListPanelGroup group)
                {
                    group.ContentPanel!.IsVisible = false;
                    group.HideButton!.IsVisible = false;
                    group.ShowButton!.IsVisible = true;

                    group.DeleteButton!.IsVisible = false;
                    group.SeeAllButton!.IsVisible = false;
                }
            }

            private void ShowPanel(object? sender, RoutedEventArgs e)
            {
                if (sender is Button button && button.Tag is ListPanelGroup group)
                {
                    group.ContentPanel!.IsVisible = true;
                    group.HideButton!.IsVisible = true;
                    group.ShowButton!.IsVisible = false;

                    group.DeleteButton!.IsVisible = true;
                    group.SeeAllButton!.IsVisible = true;
                }
            }

        #endregion
        private class ListPanelGroup
        {
            public StackPanel? ContentPanel { get; set; }
            public Button? ShowButton { get; set; }
            public Button? HideButton { get; set; }
            public Button? DeleteButton { get; set; }
            public Button? SeeAllButton { get; set; }

        }

    }
}