using System;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Collections.Generic;
using Dapper;
using System.Threading.Tasks;

namespace CineLog.Views.Helper
{
    public static class DatabaseHandler
    {
        private static readonly string dbPath = "/home/legion/CLionProjects/pyScraper/scraper/cinelog.db";
        private static readonly string connectionString = $"Data Source={dbPath};Version=3;";

        public static List<Movie> GetMovies(string? listName = null, int count = 20, int offset = 0, FilterSettings? filterSettings = null)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode=WAL;";
            command.ExecuteNonQuery();

            // Check if the titles_table exists
            var tableExists = connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='titles_table';"
            );

            if (tableExists == 0) return [];

            var query = new StringBuilder();
            var parameters = new DynamicParameters();

            // Base SELECT
            query.Append(@"
                SELECT t.title_id, t.title_name, t.poster_url 
                FROM titles_table t");

            var whereClauses = new List<string>();

            // if list
            if (!string.IsNullOrEmpty(listName))
            {
                query.Append(@"
                    JOIN list_movies_table lm ON t.title_id = lm.movie_id
                    JOIN lists_table l ON lm.list_id = l.id");
                whereClauses.Add("l.name = @ListName");
                parameters.Add("ListName", listName);
            }
                
            if (string.IsNullOrEmpty(listName)) whereClauses.Add("1 = 1"); // Dummy condition to start WHERE block

            if (filterSettings != null)
            {
                appendFilterClauses(filterSettings, whereClauses, parameters);
            }

            if (whereClauses.Count > 0)
                query.Append(" WHERE " + string.Join(" AND ", whereClauses));

            query.Append(" LIMIT @Count OFFSET @Offset");
            parameters.Add("Count", count);
            parameters.Add("Offset", offset);

            var result = connection.Query<(string, string, string)>(query.ToString(), parameters)
                .Select(tuple => new Movie(tuple.Item1, tuple.Item2, tuple.Item3))
                .ToList();

            return result;
        }

        private static void appendFilterClauses(FilterSettings filterSettings, List<string> whereClauses, DynamicParameters parameters)
        {
            whereClauses.Add("(t.rating >= @MinRating OR t.rating IS NULL)");
            whereClauses.Add("(t.rating <= @MaxRating OR t.rating IS NULL)");
            parameters.Add("MinRating", filterSettings.MinRating ?? 0);
            parameters.Add("MaxRating", filterSettings.MaxRating ?? 10);

            whereClauses.Add("(t.year_start >= @MinYear OR t.year_start IS NULL)");
            whereClauses.Add("(t.year_end <= @MaxYear OR t.year_end IS NULL)");
            parameters.Add("MinYear", filterSettings.YearStart);
            parameters.Add("MaxYear", filterSettings.YearEnd);

            if (filterSettings.Genre is { Count: > 0 })
            {
                whereClauses.Add("t.genre IN @Genres");
                parameters.Add("Genres", filterSettings.Genre);
            }

            if (!string.IsNullOrEmpty(filterSettings.Company))
            {
                whereClauses.Add("t.companies LIKE @Company");
                parameters.Add("Company", "%" + filterSettings.Company + "%");
            }

            if (!string.IsNullOrEmpty(filterSettings.Type))
            {
                whereClauses.Add("t.title_type IN @Types");
                parameters.Add("Types", filterSettings.Type.Split(','));
            }
        }

        public static List<string> GetListsFromDatabase()
        {
            List<string> lists = [];
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            using var command = new SQLiteCommand("SELECT name FROM lists_table;", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lists.Add(reader.GetString(0));
            }

            return lists;
        }

        public static void AddMovieToList(string listName, string movieId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                int listId = connection.ExecuteScalar<int>(
                    "SELECT id FROM lists_table WHERE name = @ListName",
                    new { ListName = listName }, transaction
                );

                int exists = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM list_movies_table WHERE list_id = @ListId AND movie_id = @MovieId",
                    new { ListId = listId, MovieId = movieId }, transaction
                );

                if (exists == 0)
                {
                    connection.Execute(
                        "INSERT INTO list_movies_table (list_id, movie_id) VALUES (@ListId, @MovieId)",
                        new { ListId = listId, MovieId = movieId }, transaction
                    );
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error adding movie to list: {ex}");
            }
        }

        public static void RemoveMovieFromList(string listName, string movieId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                string query = @"
                    DELETE FROM list_movies_table 
                    WHERE list_id IN (SELECT id FROM lists_table WHERE name = @ListName)
                    AND movie_id = @MovieId";

                connection.Execute(query, new { ListName = listName, MovieId = movieId });
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error removing movie from list: {ex.Message}");
            }
        }

        public static bool IsMovieInList(string listName, string movieId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT COUNT(*) 
                FROM list_movies_table lm
                JOIN lists_table l ON lm.list_id = l.id
                WHERE l.name = @ListName AND lm.movie_id = @MovieId";

            int count = connection.ExecuteScalar<int>(query, new { ListName = listName, MovieId = movieId });
            return count > 0;
        }

        public static void CreateListsTable() {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS lists_table (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE
                );
            ");

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS list_movies_table (
                    list_id INTEGER,
                    movie_id INTEGER,
                    FOREIGN KEY (list_id) REFERENCES lists_table(id),
                    FOREIGN KEY (movie_id) REFERENCES titles_table(id),
                    PRIMARY KEY (list_id, movie_id)
                );
            ");
        }

        public static string CreateNewList() 
        {
            string listName = $"CustomList#{GetNextListId()}";

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            connection.Execute("INSERT INTO lists_table (name) VALUES (@name)", new { name = listName });

            return listName;
        }

        public static void DeleteList(string listName)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            connection.Execute("DELETE FROM lists_table WHERE name = @name", new { name = listName });
        }

        private static int GetNextListId()
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            return connection.ExecuteScalar<int>("SELECT COALESCE(MAX(id), 0) + 1 FROM lists_table");
        }

        public static async Task<TitleInfo> GetTitleInfo(string id)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string checkQuery = "SELECT updated FROM titles_table WHERE title_id = @id";
            bool isUpdated = connection.ExecuteScalar<bool>(checkQuery, new { id });

            if (!isUpdated)
            {
                await ServerHandler.ScrapeSingleTitle(id);
            }

            string query = @"SELECT * FROM titles_table WHERE title_id = @id";
            var result = connection.QuerySingleOrDefault<TitleInfo>(query, new { id });

            return result;
        }

        public class FilterSettings
        {
            public float? MinRating { get; set; } = 0;
            public float? MaxRating { get; set; } = 10;
            public List<string>? Genre { get; set; } = [];
            public int YearStart { get; set; } = 1874;
            public int YearEnd { get; set; } = DateTime.Now.Year + 1;
            public string? Company { get; set; }
            public string? Type { get; set; }

            public FilterSettings() { }
        }

        public struct TitleInfo
        {
            public string Title_Id { get; set; }
            public string? Title_name { get; set; }
            public string? Poster_url { get; set; }
            public int? Year_start { get; set; }
            public int? Year_end { get; set; }
            public string? Plot { get; set; }
            public string? Runtime { get; set; }
            public string? Rating { get; set; }
            public string? Genres { get; set; }
            public string? Stars { get; set; }
            public string? Writers { get; set; }
            public string? Directors { get; set; }
            public string? Creators { get; set; }
            public string? Companies { get; set; }
        }
    }
}