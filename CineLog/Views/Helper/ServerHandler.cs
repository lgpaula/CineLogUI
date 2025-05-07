using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;

namespace CineLog.Views.Helper
{
    public static class ServerHandler
    {
        public static async Task<string> ScrapeMultipleTitles(string criteria, int? quantity)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            try
            {
                var response = await client.GetAsync($"http://127.0.0.1:5000/scrape?criteria={Uri.EscapeDataString(criteria)}&quantity={quantity}");
                string result = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("ScrapeMultipleTitles completed successfully.");
                    EventAggregator.Instance.Publish(new NotificationEvent { Message = "✅ Scraping done successfully!" });

                    if (Application.Current is App app)
                    {
                        Console.WriteLine("Restarting thread");
                        app.RestartWorkerThreads();

                    }
                    return result;
                }

                return $"Flask Error: {result}";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calling Flask API: " + ex.Message);
                return "Error";
            }
        }

        public static async Task<string> ScrapeSingleTitle(string title_id) // takes about 8 seconds per title
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync($"http://127.0.0.1:5000/scrape/{title_id}", null);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("ScrapeSingleTitle completed successfully.");
                    return result;
                }

                return $"Flask Error: {result}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scraper call failed: {ex.Message}");
                return "Error";
            }
        }

        public static async Task<string> FetchEpisodesDates(string title_id, string season_count)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"http://127.0.0.1:5000/fetch_episodes?title_id={title_id}&season_count={season_count}");
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("FetchEpisodesDates completed successfully.");
                    return result;
                }

                return $"Flask Error: {result}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fetch dates failed: {ex.Message}");
                return "Error";
            }
        }
    }
}