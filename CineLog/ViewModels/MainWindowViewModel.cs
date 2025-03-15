// handle logic, button clicks and binds data to the UI
using System;
using Avalonia.Controls;
using System.Reactive;
using ReactiveUI;
using CineLog.Views;
using Avalonia.Threading;

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
            switch (viewName)
            {
                case "Home":
                    CurrentView = new HomeView();
                    break;
                case "Scraper":
                    CurrentView = new ScraperView();
                    break;
                case "Collection":
                    CurrentView = new CollectionView();
                    break;
                default:
                    Console.WriteLine("Unknown view");
                    break;
            }
        }
    }
}
