using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Input;

namespace CineLog.Views 
{
    public class Movie
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string PosterUrl { get; set; }

        public Movie(string id, string title, string posterUrl)
        {
            Id = id;
            Title = title;
            PosterUrl = posterUrl;
        }

        public Movie() 
        {
            Id = string.Empty;
            Title = string.Empty;
            PosterUrl = string.Empty;
        }

        public Button CreateMovieButton(HttpClient httpClient)
        {
            Button movieButton = new()
            {
                Padding = new Thickness(0),
                Margin = new Thickness(10),
                Width = 150,
                Height = 250,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            Border movieBox = new()
            {
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5),
                Background = new SolidColorBrush(Color.Parse("#222222"))
            };

            StackPanel contentPanel = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5
            };

            Image movieImage = new()
            {
                Stretch = Stretch.UniformToFill,
                Width = 130,
                Height = 180
            };

            _ = LoadImageFromUrl(movieImage, httpClient);

            Border imageBorder = new()
            {
                CornerRadius = new CornerRadius(5),
                ClipToBounds = true,
                Child = movieImage
            };

            TextBlock movieTitle = new()
            {
                Text = Title,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 14,
                MaxLines = 2,
                Margin = new Thickness(0, 5, 0, 0)
            };

            contentPanel.Children.Add(imageBorder);
            contentPanel.Children.Add(movieTitle);
            movieBox.Child = contentPanel;
            movieButton.Content = movieBox;

            movieButton.Click += (s, e) => {
                Console.WriteLine($"Movie clicked: {Title}");
            };

            movieButton.PointerPressed += (s, e) =>
            {
                var contextMenu = CreateContextMenu();
                movieButton.ContextMenu = contextMenu;
                contextMenu.Open(movieButton);
            };

            return movieButton;
        }

        private ContextMenu CreateContextMenu()
        {
            ContextMenu contextMenu = new();

            var calendarMenuItem = new MenuItem { Header = "Add to Calendar" };

            var listSubMenu = new MenuItem { Header = "Add/Remove from Lists" };
            List<string> listsInDb = DatabaseHandler.GetListsFromDatabase();

            foreach (var listName in listsInDb)
            {
                var checkBox = new CheckBox
                {
                    IsChecked = DatabaseHandler.IsMovieInList(listName, Id),
                    Margin = new Thickness(0, 0, 2, 0)
                };

                checkBox.IsCheckedChanged += (sender, e) => 
                {
                    var isChecked = checkBox.IsChecked ?? false;
                    OnListCheckChanged(listName, isChecked);
                };

                var menuItem = new MenuItem
                {
                    Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            checkBox,
                            new TextBlock 
                            { 
                                Text = listName,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    },
                };

                listSubMenu.Items.Add(menuItem);
            }

            contextMenu.Items.Add(calendarMenuItem);
            contextMenu.Items.Add(listSubMenu);

            return contextMenu;
        }

        private void OnListCheckChanged(string listName, bool isChecked)
        {
            if (isChecked)
            {
                Console.WriteLine($"Added to list: {listName}");
                DatabaseHandler.AddMovieToList(listName, Id);
            }
            else
            {
                Console.WriteLine($"Removed from list: {listName}");
                DatabaseHandler.RemoveMovieFromList(listName, Id);
            }

            EventAggregator.Instance.Publish("ListUpdated", listName, listName);
        }

        private async Task LoadImageFromUrl(Image image, HttpClient httpClient)
        {
            try
            {
                using var response = await httpClient.GetAsync(PosterUrl);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                image.Source = new Bitmap(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image from {PosterUrl}: {ex.Message}");
                // Set a placeholder image 
                image.Source = null;
                // If you have a placeholder image, you would set it here
            }
        }
    }
}