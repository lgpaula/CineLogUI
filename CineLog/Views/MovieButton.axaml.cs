using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

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

            // Create a Button to make the item clickable
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

            ContextMenu contextMenu = new()
            {
                Items = 
                {
                    new MenuItem { Header = "View Details"},
                    new MenuItem { Header = "Add to Favorites"}
                }
            };
            movieButton.ContextMenu = contextMenu;

            // Add click handler
            movieButton.Click += (s, e) => {
                Console.WriteLine($"Movie clicked: {Title}");
                // Here you would navigate to the movie details
            };

            movieButton.PointerPressed += (s, e) =>
            {
                var point = e.GetCurrentPoint(movieButton);
                Console.WriteLine($"Right click: {point.Properties.IsRightButtonPressed}");

                if (point.Properties.IsRightButtonPressed)
                {
                    Console.WriteLine("Right-click detected!");
                    Console.WriteLine($"Context Menu: {contextMenu != null}");

                    if (contextMenu != null)
                    {
                        // Explicitly open the context menu at the button's location
                        contextMenu.Open(movieButton);
                    }
                }
            };

            return movieButton;
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