using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace CineLog
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();

                Styles.Add(new Style(x => x.OfType<Button>())
                {
                    Setters =
                    {
                        new Setter(Button.CursorProperty, new Cursor(StandardCursorType.Hand))
                    }
                });
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}