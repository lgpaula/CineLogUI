using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Dapper;

namespace CineLog.Views
{
    public static class DatabaseHandler
    {
        private static readonly string dbPath = "example.db";
        private static readonly string connectionString = $"Data Source={dbPath};Version=3;";
        private const int moviesPerPage = 80;

        public static List<Movie> GetMovies(string? listName = null, int count = 20, int offset = 0, FilterSettings? filterSettings = null)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            /* before filterSettings implementation
            if (string.IsNullOrEmpty(listName))
            {
                query = @"
                    SELECT title_id, title_name, poster_url 
                    FROM titles_table 
                    LIMIT @Count OFFSET @Offset";
                parameters = new { Count = count, Offset = offset };
            }
            else
            {
                query = @"
                    SELECT t.title_id, t.title_name, t.poster_url
                    FROM titles_table t
                    JOIN list_movies_table lm ON t.title_id = lm.movie_id
                    JOIN lists_table l ON lm.list_id = l.id
                    WHERE l.name = @ListName
                    LIMIT @Count OFFSET @Offset";
                parameters = new { Count = count, Offset = offset, ListName = listName };
            }

            var result = connection.Query<(string, string, string)>(query, parameters)
                        .Select(tuple => new Movie(tuple.Item1, tuple.Item2, tuple.Item3))
                        .ToList();
            */

            // If filterSettings is null, create a new one with default values
            filterSettings ??= new FilterSettings();

            // Base query
            var query = @"
                SELECT t.title_id, t.title_name, t.poster_url
                FROM titles_table t
                LEFT JOIN list_movies_table lm ON t.title_id = lm.movie_id
                LEFT JOIN lists_table l ON lm.list_id = l.id
                WHERE (@ListName IS NULL OR l.name = @ListName)";

            // Apply filters dynamically based on non-null filterSettings
            if (filterSettings.Rating != null)
                query += " AND t.rating BETWEEN @MinRating AND @MaxRating";
            if (filterSettings.Genre != null && filterSettings.Genre.Count > 0)
                query += " AND t.genre IN @Genres";
            if (filterSettings.Year != null)
                query += " AND t.release_year BETWEEN @MinYear AND @MaxYear";
            if (!string.IsNullOrEmpty(filterSettings.Company))
                query += " AND t.production_company LIKE @Company";
            if (!string.IsNullOrEmpty(filterSettings.Type))
                query += " AND t.type IN (@Types)";

            query += " LIMIT @Count OFFSET @Offset";

            // Construct query parameters with null-safe default values
            var parameters = new
            {
                MinRating = filterSettings.Rating?.Item1 ?? 0,
                MaxRating = filterSettings.Rating?.Item2 ?? 10,
                Genres = filterSettings.Genre ?? new List<string>(),
                MinYear = filterSettings.Year?.Item1 ?? 1874,
                MaxYear = filterSettings.Year?.Item2 ?? DateTime.Now.Year,
                Company = "%" + (filterSettings.Company ?? "") + "%",
                Types = filterSettings.Type?.Split(',') ?? new string[0],
                Count = count,
                Offset = offset,
                ListName = listName,
            };

            // Execute query and map the results
            var result = connection.Query<(string, string, string)>(query, parameters)
                .Select(tuple => new Movie(tuple.Item1, tuple.Item2, tuple.Item3))
                .ToList();

            return result;
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
                // Get list ID
                int listId = connection.ExecuteScalar<int>(
                    "SELECT id FROM lists_table WHERE name = @ListName",
                    new { ListName = listName }, transaction // Pass transaction here
                );

                // Check if the movie is already in the list
                int exists = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM list_movies_table WHERE list_id = @ListId AND movie_id = @MovieId",
                    new { ListId = listId, MovieId = movieId }, transaction // Pass transaction here
                );

                if (exists == 0)
                {
                    // Insert movie into list_movies_table
                    connection.Execute(
                        "INSERT INTO list_movies_table (list_id, movie_id) VALUES (@ListId, @MovieId)",
                        new { ListId = listId, MovieId = movieId }, transaction // Pass transaction here
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

            Console.WriteLine("listname: " + listName + " created");

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

        public class FilterSettings
        {
            public Tuple<float, float>? Rating { get; set; }  // Nullable for optional filtering
            public List<string>? Genre { get; set; } = new List<string>();
            public Tuple<int, int>? Year { get; set; }
            public string? Company { get; set; }
            public string? Type { get; set; }

            // Optional Constructor (empty initialization)
            public FilterSettings() { }
        }
    }
}