// handle logic, button clicks and binds data to the UI
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace CineLog.ViewModels;

public class MainWindowViewModel : ReactiveObject
{

    // ObservableCollection is like a List, but updates the UI when changed
    public ObservableCollection<string> Buttons { get; } = new()
    {
        "Button 1", "Button 2", "Button 3", "Button 4", "Button 5"
    };

    public void HandleButtonClick()
    {
        Console.WriteLine("Button clicked from ViewModel");
    }
}
