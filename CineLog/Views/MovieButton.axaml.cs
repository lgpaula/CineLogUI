using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Net.Http;
using Avalonia;
using Avalonia.Layout;

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

        public async Task<Button> CreateMovieButton(HttpClient httpClient)
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

            await LoadImageFromUrl(movieImage, httpClient);

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


            // Flyout for the ellipsis button
            // Flyout flyout = new();
            // Border flyoutBorder = new()
            // {
            //     Background = Brushes.Black,
            //     BorderBrush = Brushes.White,
            //     BorderThickness = new Thickness(2),
            //     CornerRadius = new CornerRadius(10),
            //     Padding = new Thickness(5)
            // };

            // StackPanel flyoutPanel = new()
            // {
            //     Spacing = 5,
            //     Width = 200
            // };

            // // Create the "Add to list" expander
            // var addToListExpander = new Expander
            // {
            //     Header = new TextBlock 
            //     {
            //         Text = "Add to List",
            //         Foreground = Brushes.White
            //     },
            //     IsExpanded = true,
            //     HorizontalAlignment = HorizontalAlignment.Stretch,
            //     Foreground = Brushes.White,
            //     Background = Brushes.Transparent
            // };

            // // Create a panel for checkboxes
            // var checkboxesPanel = new StackPanel
            // {
            //     Margin = new Thickness(5)
            // };
            
            // // Add the checkboxes to the panel
            // foreach (var listName in GetListsFromDatabase())
            // {
            //     // Create a wrapper panel for checkbox and label
            //     var checkboxWrapper = new StackPanel
            //     {
            //         Orientation = Orientation.Horizontal,
            //         Margin = new Thickness(5, 2, 5, 2)
            //     };
                
            //     // Create a checkbox with explicit styling
            //     CheckBox listCheckBox = new()
            //     {
            //         Name = $"checkbox_{listName.Replace(" ", "X")}",  // Unique name for the checkbox
            //         Foreground = Brushes.White,
            //         Background = Brushes.Transparent,
            //         BorderBrush = Brushes.White,
            //         BorderThickness = new Thickness(1),
            //         Margin = new Thickness(0, 0, 5, 0),
            //         IsChecked = true,
            //         MinWidth = 16,
            //         MinHeight = 16,
            //     };
                
            //     // Create a separate text label
            //     TextBlock checkBoxLabel = new()
            //     {
            //         Text = listName,
            //         Foreground = Brushes.White,
            //         VerticalAlignment = VerticalAlignment.Center
            //     };
                
            //     // Add both to the wrapper panel
            //     checkboxWrapper.Children.Add(listCheckBox);
            //     checkboxWrapper.Children.Add(checkBoxLabel);
                
            //     // Handle checkbox toggle logic
            //     listCheckBox.IsCheckedChanged += (s, e) =>
            //     {
            //         listCheckBox.InvalidateVisual();

            //         if (listCheckBox.IsChecked == true)
            //         {
            //             listCheckBox.Foreground = Brushes.LightGreen;
            //             // Uncomment and implement this method
            //             // AddMovieToList(listName, movieId);
            //             Console.WriteLine("Adding movie to list: " + listName);
            //         }
            //         else
            //         {
            //             listCheckBox.Foreground = Brushes.White;
            //             // Uncomment and implement this method
            //             // RemoveMovieFromList(listName, movieId);
            //             Console.WriteLine("Removing movie from list: " + listName);
            //         }
            //     };
                
            //     // Add the wrapper to the main panel
            //     checkboxesPanel.Children.Add(checkboxWrapper);
            // }

            // // Set the checkboxes panel as the content of the expander
            // addToListExpander.Content = checkboxesPanel;
            // flyoutPanel.Children.Add(addToListExpander);

            // // Wrap the flyoutPanel inside the flyoutBorder
            // flyoutBorder.Child = flyoutPanel;
            // flyout.Content = flyoutBorder;

            // // Ellipsis button with the Flyout
            // Button ellipsisButton = new()
            // {
            //     Content = "⋮",
            //     FontSize = 16,
            //     Foreground = Brushes.White,
            //     Background = Brushes.Transparent,
            //     BorderBrush = Brushes.Transparent,
            //     HorizontalAlignment = HorizontalAlignment.Right,
            //     Cursor = new Cursor(StandardCursorType.Hand),
            //     Flyout = flyout
            // };

            // DockPanel headerPanel = new();
            // DockPanel.SetDock(ellipsisButton, Dock.Right);
            // headerPanel.Children.Add(ellipsisButton);

            // contentPanel.Children.Add(headerPanel);

            contentPanel.Children.Add(imageBorder);
            contentPanel.Children.Add(movieTitle);
            movieBox.Child = contentPanel;
            movieButton.Content = movieBox;

            // Add click handler
            movieButton.Click += (s, e) => {
                Console.WriteLine($"Movie clicked: {Title}");
                // Here you would navigate to the movie details
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