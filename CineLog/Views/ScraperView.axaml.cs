using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls;
using Newtonsoft.Json;
using CineLog.Views.Helper;
using Avalonia;
using System;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;

namespace CineLog.Views;

public partial class ScraperView : UserControl
{
    private List<CheckBox>? _genreCheckBoxes;
    private List<CheckBox>? _typeCheckBoxes;

    public ScraperView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnLoaded;

        IncreaseButton.Click += (_, _) => ChangeQuantity(50);
        DecreaseButton.Click += (_, _) => ChangeQuantity(-50);
    }

    private void OnLoaded(object? sender, VisualTreeAttachmentEventArgs e)
    {
        var genresPanel = this.FindControl<WrapPanel>("GenresPanel");
        var typePanel = this.FindControl<WrapPanel>("TitleTypePanel");

        _genreCheckBoxes = GetCheckBoxes(genresPanel!);
        _typeCheckBoxes = GetCheckBoxes(typePanel!);
    }

    private void OnScrapeButtonClick(object? sender, RoutedEventArgs e)
    {
        var types = GetSelectedCheckBoxes(_typeCheckBoxes!);
        if (types.Count == 0)
        {
            types = [.. _typeCheckBoxes!.Select(cb => cb.Content?.ToString() ?? "")];
        }
        var criteria = new ScraperCriteria
        {
            Genres = GetSelectedCheckBoxes(_genreCheckBoxes!),
            Types = types,
            Companies = GetSelectedIds(FilterTypes.Company),
            Names = GetSelectedIds(FilterTypes.People),
            YearFrom = TryParseInt(YearStart.Text),
            YearTo = TryParseInt(YearEnd.Text),
            RatingFrom = TryParseFloat(MinRating.Text),
            RatingTo = TryParseFloat(MaxRating.Text)
        };

        _ = StartScraping(criteria, TryParseInt(Quantity.Text));

        EventAggregator.Instance.Publish(new NotificationEvent { Message = $"✅ Scraping started. {Time.Text}. Please wait." });
        ScrapeButton.IsEnabled = false;
        App.Logger?.Information($"Scraping started with criteria: {JsonConvert.SerializeObject(criteria)}");
    }

    private void OnAddExtraFilterClick(object? sender, RoutedEventArgs e)
    {
        var parentPanel = this.FindControl<StackPanel>("ExtraFilterPanel");

        var rowPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(5)),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(5)),
                new ColumnDefinition(GridLength.Auto)
            },
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(new GridLength(5)),
                new RowDefinition(GridLength.Auto)
            }
        };

        var comboBox = new ComboBox
        {
            Width = 120,
            ItemsSource = Enum.GetValues<FilterTypes>()
        };
        Grid.SetColumn(comboBox, 0);
        Grid.SetRow(comboBox, 0);

        var textBox = new TextBox
        {
            Width = 200,
            Watermark = "Type to search...",
            IsEnabled = false
        };
        Grid.SetColumn(textBox, 2);
        Grid.SetRow(textBox, 0);

        var deleteButton = new Button
        {
            Content = "X"
        };
        Grid.SetColumn(deleteButton, 4);
        Grid.SetRow(deleteButton, 0);

        var suggestions = new ObservableCollection<IdNameItem>();

        var suggestionsPanel = new ItemsControl
        {
            Background = Brushes.White,
            Foreground = Brushes.Black,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Width = 200,
            MaxHeight = 100,
            Margin = new Thickness(0, 2, 0, 0),
            ItemsSource = suggestions,
            IsVisible = false,
        };
        suggestionsPanel.ItemTemplate = new FuncDataTemplate<IdNameItem>((item, _) =>
        {
            var textBlock = new TextBlock
            {
                Margin = new Thickness(5),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            textBlock.Bind(TextBlock.TextProperty, new Binding("Name"));
            textBlock.PointerPressed += (_, _) =>
            {
                if (!string.IsNullOrEmpty(item.Id))
                {
                    textBox.Text = item.Name;
                    textBox.Tag = item.Id;
                }
                suggestions.Clear();
                suggestionsPanel.IsVisible = false;
            };
            return textBlock;
        });

        Grid.SetColumn(suggestionsPanel, 2);
        Grid.SetRow(suggestionsPanel, 2);

        // Enable text box only when an option is selected
        comboBox.SelectionChanged += (_, _) =>
        {
            textBox.IsEnabled = comboBox.SelectedItem != null;
            suggestions.Clear();
            suggestionsPanel.IsVisible = false;
        };

        // Text change logic
        textBox.GetObservable(TextBox.TextProperty).Subscribe(async text =>
        {
            var selected = comboBox.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selected) && text?.Length >= 3)
            {
                var results = await DatabaseHandler.QueryDatabaseAsync(selected, text);
                suggestions.Clear();

                if (results.Count > 0)
                {
                    foreach (var (id, name) in results)
                    {
                        suggestions.Add(new IdNameItem { Id = id, Name = name });
                    }
                }
                else
                {
                    suggestions.Add(new IdNameItem { Id = "", Name = $"No results for \"{text}\"" });
                }

                suggestionsPanel.IsVisible = true;
            }
            else
            {
                suggestions.Clear();
                suggestionsPanel.IsVisible = false;
            }
        });

        deleteButton.Click += (_, _) =>
        {
            parentPanel!.Children.Remove(rowPanel);
        };

        grid.Children.Add(comboBox);
        grid.Children.Add(textBox);
        grid.Children.Add(deleteButton);
        grid.Children.Add(suggestionsPanel);

        rowPanel.Children.Add(grid);
        parentPanel!.Children.Add(rowPanel);
    }

    private List<string> GetSelectedIds(FilterTypes type)
    {
        var parentPanel = this.FindControl<StackPanel>("ExtraFilterPanel");
        var ids = new List<string>();

        foreach (var row in parentPanel!.Children.OfType<StackPanel>())
        {
            var grid = row.Children.OfType<Grid>().FirstOrDefault();
            if (grid is null) continue;

            var comboBox = grid.Children.OfType<ComboBox>().FirstOrDefault();
            var textBox = grid.Children.OfType<TextBox>().FirstOrDefault();

            if (comboBox?.SelectedItem is not FilterTypes selectedType || selectedType != type ||
                string.IsNullOrWhiteSpace(textBox?.Text)) continue;

            var id = textBox.Tag?.ToString();
            if (!string.IsNullOrEmpty(id))
                ids.Add(id);
        }

        return ids;
    }

    private static List<CheckBox> GetCheckBoxes(WrapPanel panel)
    {
        return [.. panel.Children.OfType<CheckBox>()];
    }

    private async Task StartScraping(ScraperCriteria criteria, int? quantity)
    {
        var stringCriteria = ConvertCriteria(criteria);
        await ServerHandler.ScrapeMultipleTitles(stringCriteria, quantity);

        ScrapeButton.IsEnabled = true;
    }

    private static List<string> GetSelectedCheckBoxes(List<CheckBox> checkBoxes)
    {
        return [.. checkBoxes.Where(cb => cb.IsChecked == true).Select(cb => cb.Content?.ToString() ?? "")];
    }

    private static int? TryParseInt(string? text)
    {
        return int.TryParse(text, out var result) ? result : null;
    }

    private static float? TryParseFloat(string? text)
    {
        return float.TryParse(text, out var result) ? result : null;
    }

    private static string ConvertCriteria(ScraperCriteria criteria)
    {
        var dict = new Dictionary<string, object?>
        {
            { "genres", criteria.Genres },
            { "companies", criteria.Companies },
            { "role", criteria.Names },
            { "types", criteria.Types },
            { "yearFrom", criteria.YearFrom },
            { "yearTo", criteria.YearTo },
            { "ratingFrom", criteria.RatingFrom },
            { "ratingTo", criteria.RatingTo }
        };

        var stringCriteria = JsonConvert.SerializeObject(dict);

        return stringCriteria;
    }

    private void ChangeQuantity(int delta)
    {
        if (!int.TryParse(Quantity.Text, out int currentQuantity)) return;
        currentQuantity += delta;
        currentQuantity = Math.Clamp(currentQuantity, 50, 1000);

        Quantity.Text = currentQuantity.ToString();

        var timeSeconds = currentQuantity / 50 * 10;
        Time.Text = $"Estimated time: {timeSeconds} seconds";
    }
}

public struct ScraperCriteria
{
    public List<string> Genres { get; init; }
    public List<string> Companies { get; init; }
    public List<string> Names { get; init; }
    public List<string> Types { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public float? RatingFrom { get; init; }
    public float? RatingTo { get; init; }
}

public class IdNameItem
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public enum FilterTypes
{
    Company,
    People
}