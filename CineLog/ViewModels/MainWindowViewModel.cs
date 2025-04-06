using System;
using ReactiveUI;
using CineLog.Views;
using Avalonia.Controls;

namespace CineLog.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private UserControl _currentView;
        public UserControl CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        public MainWindowViewModel() {
            _currentView = new HomeView(); // Default view
        }

        public void HandleButtonClick(string viewName)
        {
            Console.WriteLine($"Button clicked: {viewName}");
            CurrentView = viewName switch
            {
                "Home" => new HomeView(),
                "Scraper" => new ScraperView(),
                "Collection" => new CollectionView(),
                _ when viewName.StartsWith("tt") => new TitleView(viewName),
                _ when viewName.StartsWith("CustomList") => new CollectionView(viewName),
                _ => throw new ArgumentException("Unknown view", nameof(viewName))
            };
        }
    }
}
