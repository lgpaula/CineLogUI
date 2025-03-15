using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace CineLog.Views
{
    public partial class UserModalWindow : Window
    {
        public UserModalWindow()
        {
            // Set blur effect on host window when this dialog opens
            Opened += (sender, e) => SetBlurEffect(true);
            // Remove blur when dialog closes
            Closed += (sender, e) => SetBlurEffect(false);

            // Prevent window moving
            this.PointerPressed += (sender, e) => e.Handled = true;
            this.PointerMoved += (sender, e) => e.Handled = true;
            this.PointerReleased += (sender, e) => e.Handled = true;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close(); // Close the modal window when the button is clicked.
        }

        private void SetBlurEffect(bool enable)
        {
            if (this.Owner is Window parentWindow)
            {
                if (enable)
                {
                    // Create a blur effect and apply it to the parent window
                    var blurEffect = new BlurEffect
                    {
                        Radius = 10
                    };
                    parentWindow.Effect = blurEffect;
                    
                    // Optional: add a semi-transparent overlay on the parent
                    var overlayControl = new Border
                    {
                        Name = "ModalOverlay",
                        Background = new SolidColorBrush(Color.Parse("#80000000")),
                        IsHitTestVisible = false
                    };
                    
                    if (parentWindow.Content is Panel panel)
                    {
                        panel.Children.Add(overlayControl);
                    }
                }
                else
                {
                    // Remove the blur effect
                    parentWindow.Effect = null;
                    
                    // Remove the overlay if it exists
                    if (parentWindow.Content is Panel panel)
                    {
                        var overlay = panel.Children.FirstOrDefault(c => c is Border b && b.Name == "ModalOverlay");
                        if (overlay != null)
                        {
                            panel.Children.Remove(overlay);
                        }
                    }
                }
            }
        }
    }
}