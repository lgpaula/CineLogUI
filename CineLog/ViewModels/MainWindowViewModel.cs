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

    // Reactive Command for handling button clicks
    public ReactiveCommand<string, Unit> ButtonClickCommand { get; }

    public MainWindowViewModel()
    {
        // Define what happens when a button is clicked
        ButtonClickCommand = ReactiveCommand.Create<string>(OnButtonClick);
    }

    private void OnButtonClick(string buttonText)
    {
        Console.WriteLine($"Button clicked: {buttonText}");
    }
}
