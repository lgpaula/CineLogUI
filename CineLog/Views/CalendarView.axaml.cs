using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using Avalonia.Controls.Primitives;
using Avalonia;
using System.Text.Json;
using Avalonia.Interactivity;
using CineLog.Views.Helper;
using Avalonia.Input;

namespace CineLog.Views
{
    public partial class CalendarView : UserControl
    {
        private static UniformGrid? _calendarGrid;
        private static TextBlock? _monthLabel;
        private static DateTime _currentMonth;

        public CalendarView()
        {
            InitializeComponent();
            _calendarGrid = this.FindControl<UniformGrid>("CalendarGrid")!;
            _monthLabel = this.FindControl<TextBlock>("MonthLabel")!;
            _currentMonth = DateTime.Today;
            BuildCalendar();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public static void AddMovieToCalendar(string dateList, string titleId)
        {
            if (string.IsNullOrWhiteSpace(dateList)) return;

            string[] dates;
            try
            {
                dates = JsonSerializer.Deserialize<string[]>(dateList)!;
            }
            catch
            {
                dates = dateList.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }

            foreach (var raw in dates)
            {
                if (DateTime.TryParse(raw.Trim('"'), out var dt))
                {
                    var d = dt.Date.ToString("yyyy-MM-dd");
                    DatabaseHandler.AddMovieToDate(d, titleId);

                    if (IsInCurrentMonth(dt)) BuildCalendar();
                }
                else
                {
                    Console.WriteLine($"Invalid date format: {raw}");
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

            var firstOf = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int startOffset = ((int)firstOf.DayOfWeek + 6) % 7;
            int daysInThis = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            var prevMonth = _currentMonth.AddMonths(-1);
            int daysInPrev = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);

            // fetch all (date, title_id) in this month
            var lastOf = firstOf.AddMonths(1).AddDays(-1);
            var idByDate = DatabaseHandler.LoadEntriesForMonth(firstOf, lastOf);

            for (int i = 0; i < 42; i++)
            {
                DateTime date;
                if (i < startOffset)
                {
                    int day = daysInPrev - startOffset + i + 1;
                    date = new DateTime(prevMonth.Year, prevMonth.Month, day);
                }
                else if (i < startOffset + daysInThis)
                {
                    date = firstOf.AddDays(i - startOffset);
                }
                else
                {
                    int day = i - (startOffset + daysInThis) + 1;
                    var nxt = _currentMonth.AddMonths(1);
                    date = new DateTime(nxt.Year, nxt.Month, day);
                }

                bool isToday = date.Date == DateTime.Today;
                var border = new Border
                {
                    BorderBrush = IsInCurrentMonth(date) ? Brushes.Gray : Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4),
                    Background = isToday ? Brushes.MediumPurple : Brushes.Transparent,
                    Child = CreateDayCell(date, idByDate)
                };

                _calendarGrid.Children.Add(border);
            }
        }

        private static Grid CreateDayCell(DateTime date, Dictionary<DateTime, List<string>> map)
        {
            bool isCur = date.Month == _currentMonth.Month;

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto, *"),
                ColumnDefinitions = new ColumnDefinitions("1*"),
            };

            if (isCur)
            {
                var header = new TextBlock
                {
                    Text = date.Day.ToString(),
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                Grid.SetRow(header, 0);
                grid.Children.Add(header);
            }

            var wrapPanel = new WrapPanel { Orientation = Orientation.Horizontal };

            if (map.TryGetValue(date.Date, out var titles))
            {
                foreach (var id in titles)
                {
                    var btn = new Movie(id).CreateMovieButton(0.4);
                    btn.Cursor = new Cursor(StandardCursorType.Arrow);
                    wrapPanel.Children.Add(btn);
                }
            }

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Content = wrapPanel
            };

            Grid.SetRow(scroll, 1);
            grid.Children.Add(scroll);

            return grid;
        }

        private void PreviousMonth_Click(object? sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            BuildCalendar();
        }

        private void NextMonth_Click(object? sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            BuildCalendar();
        }
    }
}
