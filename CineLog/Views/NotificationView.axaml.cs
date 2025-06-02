using Avalonia;
using Avalonia.Controls;

namespace CineLog.Views;

public partial class NotificationView : UserControl
{
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<NotificationView, string>(nameof(Message));

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public NotificationView()
    {
        InitializeComponent();
        PointerPressed += (_, _) => Dismiss();
        AttachedToVisualTree += (_, _) =>
        {
            MessageText.Text = Message;
        };
    }

    private void Dismiss()
    {
        (Parent as Panel)?.Children.Remove(this);
    }
}

public class NotificationEvent
{
    public string Message { get; set; } = "";
}