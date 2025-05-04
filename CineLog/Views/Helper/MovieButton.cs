using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Controls.Shapes;
using Avalonia.Animation;

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

        public Button CreateMovieButton(double percentage = 1.0)
        {
            var button = new Button
            {
                Width = 140 * percentage,
                Height = 207 * percentage,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            // Gradient overlay rectangle
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                [
                    new GradientStop(Color.FromRgb(0, 0, 0), 0),
                    new GradientStop(Color.FromArgb(200, 0, 0, 0), 1)
                ]
            };

            var overlayRect = new Rectangle
            {
                Fill = gradientBrush,
                Opacity = 0,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsHitTestVisible = false,
                Transitions =
                [
                    new DoubleTransition
                    {
                        Property = Rectangle.OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(250)
                    }
                ]
            };

            var titleText = new TextBlock
            {
                Text = Title,
                Foreground = Brushes.White,
                FontSize = 16,

                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,

                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(2, 10, 2, 0),
                Opacity = 0,
                IsHitTestVisible = false,
                Transitions =
                [
                    new DoubleTransition
                    {
                        Property = Visual.OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(100)
                    }
                ]
            };

            var grid = new Grid();
            grid.Children.Add(GetImageBorder());
            grid.Children.Add(overlayRect);
            grid.Children.Add(titleText);

            button.Content = grid;

            button.PointerEntered += (_, __) =>
            {
                overlayRect.Opacity = 1;
                titleText.Opacity = 1;
            };

            button.PointerExited += (_, __) =>
            {
                overlayRect.Opacity = 0;
                titleText.Opacity = 0;
            };

            button.PointerPressed += (s, e) =>
            {
                var contextMenu = CreateContextMenu();
                button.ContextMenu = contextMenu;
                contextMenu.Open(button);
            };

            return button;
        }

        private ContextMenu CreateContextMenu()
        {
            ContextMenu contextMenu = new();

            var calendarMenuItem = new MenuItem { Header = "Add to Calendar" };

            var listSubMenu = new MenuItem { Header = "Add/Remove from Lists" };
            var customLists = DatabaseHandler.GetListsFromDatabase();

            foreach (var list in customLists)
            {
                var checkBox = new CheckBox
                {
                    IsChecked = DatabaseHandler.IsMovieInList(list.Uuid!, Id),
                    Margin = new Thickness(0, 0, 2, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                checkBox.IsCheckedChanged += (sender, e) =>
                {
                    var isChecked = checkBox.IsChecked ?? false;
                    OnListCheckChanged(list, isChecked);
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
                                Text = DatabaseHandler.GetListName(list.Uuid!),
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    },
                    StaysOpenOnClick = true // this is critical!
                };

                listSubMenu.Items.Add(menuItem);
            }

            contextMenu.Items.Add(calendarMenuItem);
            contextMenu.Items.Add(listSubMenu);

            return contextMenu;
        }

        private void OnListCheckChanged(DatabaseHandler.CustomList list, bool isChecked)
        {
            if (isChecked) DatabaseHandler.AddMovieToList(list.Uuid!, Id);
            else DatabaseHandler.RemoveMovieFromList(list.Uuid!, Id);

            EventAggregator.Instance.Publish("ListUpdated", list);
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

        public Bitmap? GetImageSource()
        {
            try
            {
                using var response = _httpClient.GetAsync(PosterUrl).Result;
                response.EnsureSuccessStatusCode();

                using var stream = response.Content.ReadAsStreamAsync().Result;
                return new Bitmap(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image from {PosterUrl}: {ex.Message}");
                return null;
            }
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