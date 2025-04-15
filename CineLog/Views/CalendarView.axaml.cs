using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using Avalonia.Controls.Primitives;
using Avalonia;

namespace CineLog.Views
{
    public partial class CalendarView : UserControl
    {
        private UniformGrid _calendarGrid;
        private TextBlock _monthLabel;
        private DateTime _currentMonth;

        // Store buttons by date
        private readonly Dictionary<DateTime, List<Control>> _buttonsByDate = new();

        public CalendarView()
        {
            InitializeComponent();
            _calendarGrid = this.FindControl<UniformGrid>("CalendarGrid")!;
            _monthLabel = this.FindControl<TextBlock>("MonthLabel")!;

            _currentMonth = DateTime.Today;
            BuildCalendar();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void AddMovieToDate(DateTime date, Control movieButton)
        {
            var key = date.Date;
            if (!_buttonsByDate.ContainsKey(key))
                _buttonsByDate[key] = [];

            _buttonsByDate[key].Add(movieButton);

            if (IsInCurrentMonth(date))
                BuildCalendar(); // Refresh display
        }

        private bool IsInCurrentMonth(DateTime date)
        {
            return date.Year == _currentMonth.Year && date.Month == _currentMonth.Month;
        }

        private void BuildCalendar()
        {
            _calendarGrid.Children.Clear();
            _monthLabel.Text = _currentMonth.ToString("MMMM yyyy");

            var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int dayOfWeekOffset = ((int)firstDay.DayOfWeek + 6) % 7;
            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            int totalCells = 42;

            for (int i = 0; i < totalCells; i++)
            {
                DateTime? date = null;
                if (i >= dayOfWeekOffset && i < dayOfWeekOffset + daysInMonth)
                    date = firstDay.AddDays(i - dayOfWeekOffset);

                bool isToday = date.HasValue && date.Value.Date == DateTime.Today;

                var border = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4),
                    Background = isToday ? Brushes.LightBlue : Brushes.Transparent,
                    Child = CreateDayCell(date)
                };

                _calendarGrid.Children.Add(border);
            }
        }

        private Control CreateDayCell(DateTime? date)
        {
            if (date == null)
                return new TextBlock(); // blank cell

            var day = date.Value;
            var stack = new StackPanel { Orientation = Orientation.Vertical };

            // Show day number
            stack.Children.Add(new TextBlock
            {
                Text = day.Day.ToString(),
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Left
            });

            // Scrollable movie buttons
            var scroll = new ScrollViewer
            {
                MaxHeight = 100,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new StackPanel { Orientation = Orientation.Vertical }
            };

            if (_buttonsByDate.TryGetValue(day.Date, out var buttons))
            {
                foreach (var btn in buttons)
                    ((StackPanel)scroll.Content).Children.Add(btn);
            }

            stack.Children.Add(scroll);
            return stack;
        }

        private void PreviousMonth_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            BuildCalendar();
        }

        private void NextMonth_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            BuildCalendar();
        }
    }
}
