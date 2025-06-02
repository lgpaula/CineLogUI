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
            EventAggregator.Instance.Subscribe<DatabaseHandler.CustomList>(LoadListUi);

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

            var collection = new DatabaseHandler.CustomList("CollectionContainer");

            LoadListUi(collection);

            var lists = DatabaseHandler.GetListsFromDatabase();
            foreach (var list in lists)
            {
                LoadListUi(list);
            }
        }

        private void LoadListUi(DatabaseHandler.CustomList customList)
        {
            var panel = _panelsList.FirstOrDefault(p => p.Name == (customList.Uuid ?? customList.Name));
            panel ??= CreateListPanel(customList);

            var sqlQuery = new DatabaseHandler.SQLQuerier {
                List_uuid = customList.Uuid,
                Limit = 20
            };

            var moviesInDatabase = DatabaseHandler.GetMovies(sqlQuery);

            var movieButtonsInUi = new Dictionary<string, Button>();
            foreach (var child in panel.Children)
            {
                if (child is Button { Tag: string movieId } button)
                    movieButtonsInUi[movieId] = button;
            }

            var movieIdsInDatabase = new HashSet<string>(moviesInDatabase.ConvertAll(m => m.Id));
            var movieIdsInUi = new HashSet<string>(movieButtonsInUi.Keys);

            foreach (var movie in moviesInDatabase)
            {
                if (movieIdsInUi.Contains(movie.Id)) continue;
                var movieButton = movie.CreateMovieButton();
                movieButton.Tag = movie.Id;
                movieButton.Cursor = new Cursor(StandardCursorType.Arrow);
                panel.Children.Add(movieButton);
            }

            foreach (var movieId in movieIdsInUi.Where(movieId => !movieIdsInDatabase.Contains(movieId)))
            {
                panel.Children.Remove(movieButtonsInUi[movieId]);
            }
        }

        private StackPanel CreateListPanel(DatabaseHandler.CustomList customList)
        {
            var listsContainer = this.FindControl<StackPanel>("CustomListsContainer")!;

            DockPanel dockPanel = new()
            {
                LastChildFill = false
            };

            TextBox listTitle = new()
            {
                Text = customList.Name,
                Foreground = Brushes.White,
                FontSize = 16,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Margin = new Thickness(0, 0, 5, 0)
            };

            listTitle.LostFocus += (_, _) =>
            {
                if (listTitle.Text == customList.Name) return;
                DatabaseHandler.UpdateListName(customList, listTitle.Text!);
                customList.Name = listTitle.Text;
            };

            StackPanel listPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Name = customList.Uuid ?? customList.Name
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
                Tag = customList.Uuid ?? customList.Name,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.White
            };
            seeAllButton.Click += ViewChanger;

            Button deleteListButton = new()
            {
                Content = "Delete",
                FontSize = 16,
                Tag = customList.Uuid ?? customList.Name,
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
                Console.WriteLine("list: " + list);
                CreateListPanel(list);
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
            public StackPanel? ContentPanel { get; init; }
            public Button? ShowButton { get; init; }
            public Button? HideButton { get; init; }
            public Button? DeleteButton { get; init; }
            public Button? SeeAllButton { get; init; }

        }

    }
}