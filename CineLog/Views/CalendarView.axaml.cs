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
        private static UniformGrid ?_calendarGrid;
        private static TextBlock ?_monthLabel;
        private static DateTime _currentMonth;

        // Store buttons by date
        private static readonly Dictionary<DateTime, List<Control>> _buttonsByDate = [];

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

        public static void AddMovieToCalendar(string dateList, Control movieButton)
        {
            if (string.IsNullOrEmpty(dateList)) return;

            var dates = dateList.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var dateStr in dates)
            {
                if (DateTime.TryParse(dateStr.Trim(), out var date))
                {
                    var key = date.Date;

                    if (!_buttonsByDate.ContainsKey(key))
                        _buttonsByDate[key] = [];

                    _buttonsByDate[key].Add(movieButton);

                    if (IsInCurrentMonth(date))
                        BuildCalendar();
                }
                else
                {
                    Console.WriteLine($"Invalid date format: {dateStr}");
                }
            }
        }

        private static bool IsInCurrentMonth(DateTime date)
        {
            return date.Year == _currentMonth.Year && date.Month == _currentMonth.Month;
        }

        private static void BuildCalendar()
        {
            _calendarGrid!.Children.Clear();
            _monthLabel!.Text = _currentMonth.ToString("MMMM yyyy");

            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int startOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            int daysInCurrentMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            var previousMonth = _currentMonth.AddMonths(-1);
            int daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);

            int totalCells = 42;

            for (int i = 0; i < totalCells; i++)
            {
                DateTime date;

                if (i < startOffset)
                {
                    // Days from previous month
                    int day = daysInPreviousMonth - startOffset + i + 1;
                    date = new DateTime(previousMonth.Year, previousMonth.Month, day);
                }
                else if (i < startOffset + daysInCurrentMonth)
                {
                    // Days from current month
                    int day = i - startOffset + 1;
                    date = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
                }
                else
                {
                    // Days from next month
                    int day = i - (startOffset + daysInCurrentMonth) + 1;
                    var nextMonth = _currentMonth.AddMonths(1);
                    date = new DateTime(nextMonth.Year, nextMonth.Month, day);
                }

                bool isToday = date.Date == DateTime.Today;

                var border = new Border
                {
                    BorderBrush = IsInCurrentMonth(date) ? Brushes.Gray : Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4),
                    Background = isToday ? Brushes.MediumPurple : Brushes.Transparent,
                    Child = CreateDayCell(date)
                };

                _calendarGrid.Children.Add(border);
            }
        }

        private static StackPanel CreateDayCell(DateTime date)
        {
            var isCurrentMonth = date.Month == _currentMonth.Month;

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            // Show day number
            if (isCurrentMonth)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = date.Day.ToString(),
                    FontWeight = FontWeight.Bold,
                    Foreground = isCurrentMonth ? Brushes.Black : Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Left
                });
            }

            // Scrollable movie buttons
            var scroll = new ScrollViewer
            {
                MaxHeight = 100,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new StackPanel { Orientation = Orientation.Vertical }
            };

            if (_buttonsByDate.TryGetValue(date.Date, out var buttons))
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
