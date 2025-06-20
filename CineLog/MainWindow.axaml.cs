// using System;
// using Avalonia;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CineLog.ViewModels;
using CineLog.Views;
using CineLog.Views.Helper;

namespace CineLog;

public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
        EventAggregator.Instance.Subscribe<NotificationEvent>(async e => await ShowNotificationAsync(e.Message));
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
        if (sender is Button { Tag: string viewName })
        {
            ViewModel.HandleButtonClick(viewName);
        }
    }

    private async Task ShowNotificationAsync(string message)
    {
        var view = new NotificationView { Message = message };
        var overlayArea = this.FindControl<StackPanel>("OverlayArea");
        overlayArea!.Children.Insert(0, view);

        await Task.Delay(TimeSpan.FromSeconds(5));
        overlayArea.Children.Remove(view);
    }
}