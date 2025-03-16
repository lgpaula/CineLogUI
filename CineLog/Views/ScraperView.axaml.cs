using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CineLog.Views
{
    public partial class ScraperView : UserControl
    {
        public ScraperView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            // You can do additional initialization here, directly on the UI thread
            Console.WriteLine("settings");
        }
    }
}
