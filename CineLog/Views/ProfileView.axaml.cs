using System;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Markup.Xaml;

namespace CineLog.Views
{
    public partial class ProfileView : UserControl
    {
        public ProfileView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            // You can do additional initialization here, directly on the UI thread
            Console.WriteLine("profile");
        }
    }
}
