using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CineLog.Views.Helper
{
    public static class ServerHandler
    {
        public static async Task<string> ScrapeMultipleTitles(string criteria)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            try
            {
                var response = await client.GetAsync($"http://127.0.0.1:5000/scrape?criteria={Uri.EscapeDataString(criteria)}");
                string result = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode) return "Flask API Error: " + result;

                return result;
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Flask API timeout: " + ex.Message);
                return "Error: Timeout";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calling Flask API: " + ex.Message);
                return "Error";
            }
        }

        public static async Task<string> ScrapeSingleTitle(string title_id)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync($"http://127.0.0.1:5000/scrape/{title_id}", null);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return result;

                return $"Flask Error: {result}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scraper call failed: {ex.Message}");
                return "Error";
            }
        }

        public static async Task<string> FetchEpisodesDates(string title_id)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync($"http://127.0.0.1:5000/fetch_episodes/{title_id}", null);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return result;

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