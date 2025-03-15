using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CineLog.ViewModels;
using CineLog.Views;

namespace CineLog
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ViewChanger(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string viewName)
            {
                ViewModel.HandleButtonClick(viewName);
            }
        }

        private async void ClickExpander(object sender, RoutedEventArgs e)
        {
            var modalWindow = new UserModalWindow();

            if (VisualRoot is Window parentWindow)
            {
                await modalWindow.ShowDialog(parentWindow);
                // Code here will execute after the modal is closed
            }
        }

    }
}