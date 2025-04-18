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

            var moviesInDatabase = DatabaseHandler.GetMovies(listId);

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
                LastChildFill = true
            };

            TextBox listTitle = new()
            {
                Text = listName,
                Foreground = Brushes.White,
                FontSize = 16,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent
            };

            listTitle.LostFocus += (s, e) =>
            {
                if (listTitle.Text != listName)
                {
                    DatabaseHandler.UpdateListName(listName, listTitle.Text);
                    listName = listTitle.Text;
                }
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
                BorderBrush = Brushes.White
            };
            deleteListButton.Click += DeleteList;

            DockPanel.SetDock(listTitle, Dock.Left);
            DockPanel.SetDock(deleteListButton, Dock.Right);
            DockPanel.SetDock(seeAllButton, Dock.Right);

            dockPanel.Children.Add(listTitle);
            dockPanel.Children.Add(seeAllButton);
            dockPanel.Children.Add(deleteListButton);

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
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(20),
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 5, 0, 0),
                Padding = new Thickness(10),
                Child = scrollViewer
            };

            listsContainer?.Children.Add(dockPanel);
            listsContainer?.Children.Add(listBorder);

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

        #endregion
    }
}