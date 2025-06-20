using System;
using ReactiveUI;
using CineLog.Views;
using Avalonia.Controls;

namespace CineLog.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private UserControl _currentView = new HomeView(); // Default view

    public UserControl CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public void HandleButtonClick(string viewName)
    {
        App.Logger?.Information($"Button clicked: {viewName}");

        CurrentView = viewName switch
        {
            "Home" => new HomeView(),
            "Scraper" => new ScraperView(),
            "Calendar" => new CalendarView(),
            "Collection" => new CollectionView(),
            _ when IsValidUuid(viewName) => new CollectionView(viewName),
            _ => throw new ArgumentException("Unknown view", nameof(viewName))
        };
    }

    private static bool IsValidUuid(string input)
    {
        return Guid.TryParse(input, out _);
    }
}