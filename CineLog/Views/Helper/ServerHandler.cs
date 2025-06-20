using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using System.Text.Json;
using System.Text;

namespace CineLog.Views.Helper;

public static class ServerHandler
{
    public static async Task ScrapeMultipleTitles(string criteriaJson, int? quantity)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(120);
        App.Logger?.Information("Starting ScrapeMultipleTitles...");

        try
        {
            var contentObj = new
            {
                criteria = JsonSerializer.Deserialize<object>(criteriaJson),
                quantity = quantity ?? 50
            };

            var contentJson = JsonSerializer.Serialize(contentObj);
            var content = new StringContent(contentJson, Encoding.UTF8, "application/json");

            App.Logger?.Information("Sending POST to /scrape with payload: {Payload}", contentJson);

            var response = await client.PostAsync("http://127.0.0.1:5000/scrape", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                App.Logger?.Error("ScrapeMultipleTitles failed with status code: {StatusCode}, response: {Response}", 
                    response.StatusCode, result);
                return;
            }

            App.Logger?.Information("ScrapeMultipleTitles succeeded. Parsing response...");

            var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);
            if (jsonResult.TryGetProperty("result", out var count))
            {
                var insertedCount = count.GetInt32();
                App.Logger?.Information("Scraped {Count} titles.", insertedCount);
                EventAggregator.Instance.Publish(new NotificationEvent 
                { 
                    Message = $"✅ Scraping done successfully! Added {insertedCount} new titles." 
                });
            }

            if (Application.Current is App app)
            {
                App.Logger?.Information("Restarting worker threads...");
                app.RestartWorkerThreads();
            }
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "Exception during ScrapeMultipleTitles");
            Console.WriteLine("Error calling Flask API: " + ex.Message);
        }
    }

    public static async Task ScrapeSingleTitle(string titleId)
    {
        App.Logger?.Information("Starting ScrapeSingleTitle for titleId: {TitleId}", titleId);

        try
        {
            using var client = new HttpClient();
            var response = await client.PostAsync($"http://127.0.0.1:5000/scrape/{titleId}", null);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                App.Logger?.Error("ScrapeSingleTitle failed for {TitleId}. Status: {Status}, Response: {Response}", 
                    titleId, response.StatusCode, result);
                return;
            }

            App.Logger?.Information("ScrapeSingleTitle completed successfully for {TitleId}.", titleId);
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "ScrapeSingleTitle failed for {TitleId}", titleId);
            Console.WriteLine($"Scraper call failed: {ex.Message}");
        }
    }

    public static async Task FetchEpisodesDates(string titleId, string seasonCount)
    {
        App.Logger?.Information("Starting FetchEpisodesDates for titleId: {TitleId}, seasonCount: {SeasonCount}", 
            titleId, seasonCount);

        try
        {
            using var client = new HttpClient();
            var url = $"http://127.0.0.1:5000/fetch_episodes?title_id={titleId}&season_count={seasonCount}";
            App.Logger?.Information("Sending GET request to: {Url}", url);

            var response = await client.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                App.Logger?.Information("FetchEpisodesDates completed successfully.");
            }
            else
            {
                App.Logger?.Error("FetchEpisodesDates failed. Status: {Status}, Response: {Response}", 
                    response.StatusCode, result);
            }
        }
        catch (Exception ex)
        {
            App.Logger?.Error(ex, "FetchEpisodesDates failed for {TitleId}", titleId);
            Console.WriteLine($"Fetch dates failed: {ex.Message}");
        }
    }
}