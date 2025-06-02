using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using System.Text.Json;
using System.Text;

namespace CineLog.Views.Helper
{
    public static class ServerHandler
    {
        public static async Task ScrapeMultipleTitles(string criteriaJson, int? quantity)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            try
            {
                var contentObj = new
                {
                    criteria = JsonSerializer.Deserialize<object>(criteriaJson),
                    quantity = quantity ?? 50
                };

                var contentJson = JsonSerializer.Serialize(contentObj);
                var content = new StringContent(contentJson, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("http://127.0.0.1:5000/scrape", content);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return;
                Console.WriteLine("ScrapeMultipleTitles completed successfully.");
                var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);
                if (jsonResult.TryGetProperty("result", out var count))
                {
                    var insertedCount = count.GetInt32();
                    Console.WriteLine($"Scraped {insertedCount} titles.");
                    EventAggregator.Instance.Publish(new NotificationEvent { Message = $"✅ Scraping done successfully! Added {insertedCount} new titles." });
                }

                if (Application.Current is not App app) return;
                Console.WriteLine("Restarting thread");
                app.RestartWorkerThreads();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calling Flask API: " + ex.Message);
            }
        }

        public static async Task ScrapeSingleTitle(string titleId) // takes about 8 seconds per title
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync($"http://127.0.0.1:5000/scrape/{titleId}", null);

                if (!response.IsSuccessStatusCode) return;
                Console.WriteLine("ScrapeSingleTitle completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scraper call failed: {ex.Message}");
            }
        }

        public static async Task FetchEpisodesDates(string titleId, string seasonCount)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"http://127.0.0.1:5000/fetch_episodes?title_id={titleId}&season_count={seasonCount}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("FetchEpisodesDates completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fetch dates failed: {ex.Message}");
            }
        }
    }
}