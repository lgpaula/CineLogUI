// using System;
// using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CineLog.ViewModels;
using CineLog.Views;
using CineLog.Views.Helper;

namespace CineLog
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

        public MainWindow()
        {
            InitializeComponent();
            EventAggregator.Instance.Subscribe<NotificationEvent>(e => ShowNotification(e.Message));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            // this.GetObservable(ClientSizeProperty).Subscribe(size =>
            // {
            //     Console.WriteLine($"Window resized: Width = {size.Width}, Height = {size.Height}");
            // });
        }

        private void ViewChanger(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string viewName)
            {
                ViewModel.HandleButtonClick(viewName);
            }
        }

        public void ShowNotification(string message)
        {
            var view = new NotificationView { Message = message };
            var overlayArea = this.FindControl<StackPanel>("OverlayArea");
            overlayArea!.Children.Add(view);
        }
    }
}