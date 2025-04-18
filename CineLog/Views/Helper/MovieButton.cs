using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace CineLog.Views.Helper 
{
    public class Movie
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string PosterUrl { get; set; }
        private readonly HttpClient _httpClient = new();

        public Movie(string id, string title, string posterUrl)
        {
            Id = id;
            Title = title;
            PosterUrl = posterUrl;
        }

        public Movie(string id) 
        {
            Id = id;
            Title = DatabaseHandler.GetMovieTitle(id);
            PosterUrl = DatabaseHandler.GetPosterUrl(id);
        }

        public Button CreateMovieButton()
        {
            Button movieButton = new()
            {
                Padding = new Thickness(0),
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                Width = 140,
                Height = 207
            };

            Border movieBox = new()
            {
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5),
                Background = Brushes.Transparent
            };

            StackPanel contentPanel = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 5
            };

            contentPanel.Children.Add(GetImageBorder());
            movieBox.Child = contentPanel;
            movieButton.Content = movieBox;

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
            var listsInDb = DatabaseHandler.GetListsFromDatabase();

            foreach (var (_, list_id) in listsInDb)
            {
                var checkBox = new CheckBox
                {
                    IsChecked = DatabaseHandler.IsMovieInList(list_id, Id),
                    Margin = new Thickness(0, 0, 2, 0)
                };

                checkBox.IsCheckedChanged += (sender, e) => 
                {
                    var isChecked = checkBox.IsChecked ?? false;
                    OnListCheckChanged(list_id, isChecked);
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
                                Text = DatabaseHandler.GetListName(list_id),
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

        private void OnListCheckChanged(string list_id, bool isChecked)
        {
            if (isChecked)
            {
                DatabaseHandler.AddMovieToList(list_id, Id);
            }
            else
            {
                DatabaseHandler.RemoveMovieFromList(list_id, Id);
            }

            EventAggregator.Instance.Publish("ListUpdated", DatabaseHandler.GetListName(list_id), list_id);
        }

        public Border GetImageBorder()
        {
            Image movieImage = new()
            {
                Stretch = Stretch.UniformToFill,
            };

            _ = LoadImageFromUrl(movieImage);

            Border imageBorder = new()
            {
                CornerRadius = new CornerRadius(5),
                ClipToBounds = true,
                Child = movieImage
            };

            return imageBorder;
        }

        private async Task LoadImageFromUrl(Image image)
        {
            try
            {
                using var response = await _httpClient.GetAsync(PosterUrl);
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