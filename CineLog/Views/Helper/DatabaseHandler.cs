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

        public static List<Movie> GetMovies(string? list_uuid = null, int limit = -1, int offset = 0, FilterSettings? filterSettings = null)
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

            // Components of the query
            var whereClauses = new List<string>();
            var joinClauses = new List<string>();

            // Base SELECT
            query.Append(@"
                SELECT DISTINCT t.title_id, t.title_name, t.poster_url 
                FROM titles_table t");

            // If list
            if (!string.IsNullOrEmpty(list_uuid))
            {
                joinClauses.Add("JOIN list_movies_table lm ON t.title_id = lm.movie_id");
                joinClauses.Add("JOIN lists_table l ON lm.list_id = l.uuid");
                whereClauses.Add("l.uuid = @ListId");
                parameters.Add("ListId", list_uuid);
            }
            else whereClauses.Add("1 = 1");

            // Apply filters and collect join clauses
            if (filterSettings != null) AppendFilterClauses(filterSettings, whereClauses, parameters, joinClauses);

            foreach (var join in joinClauses) query.Append("\n" + join);

            if (whereClauses.Count > 0) query.Append("\nWHERE " + string.Join(" AND ", whereClauses));

            if (filterSettings != null) query.Append("\nORDER BY " + filterSettings.SortBy);
            else query.Append("\nORDER BY t.created_on DESC");

            query.Append("\nLIMIT @Limit OFFSET @Offset");
            parameters.Add("Limit", limit);
            parameters.Add("Offset", offset);

            var result = connection.Query<(string, string, string)>(query.ToString(), parameters)
                .Select(tuple => new Movie(tuple.Item1, tuple.Item2, tuple.Item3))
                .ToList();

            return result;
        }

        private static void AppendFilterClauses(FilterSettings filterSettings, List<string> whereClauses, DynamicParameters parameters, List<string> joins)
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
                joins.Add("JOIN title_genre tg ON tg.title_id = t.title_id");
                joins.Add("JOIN genres_table g ON g.id = tg.genres_id");
                whereClauses.Add("g.id IN @Genres");
                parameters.Add("Genres", filterSettings.Genre);
            }

            if (filterSettings.Company is { Count: > 0 })
            {
                joins.Add("JOIN title_company tc ON tc.title_id = t.title_id");
                joins.Add("JOIN companies_table c ON c.id = tc.companies_id");
                whereClauses.Add("c.id IN @Companies");
                parameters.Add("Companies", filterSettings.Company);
            }

            if (!string.IsNullOrEmpty(filterSettings.Type))
            {
                whereClauses.Add("t.title_type IN @Types");
                parameters.Add("Types", filterSettings.Type.Split(','));
            }

            if (!string.IsNullOrWhiteSpace(filterSettings.SearchTerm))
            {
                joins.Add("LEFT JOIN title_cast ts ON ts.title_id = t.title_id");
                joins.Add("LEFT JOIN cast_table cast_t ON cast_t.id = ts.cast_id");

                joins.Add("LEFT JOIN title_director td ON td.title_id = t.title_id");
                joins.Add("LEFT JOIN directors_table dir_t ON dir_t.id = td.directors_id");

                joins.Add("LEFT JOIN title_writer tw ON tw.title_id = t.title_id");
                joins.Add("LEFT JOIN writers_table wri_t ON wri_t.id = tw.writers_id");

                joins.Add("LEFT JOIN title_creator tcr ON tcr.title_id = t.title_id");
                joins.Add("LEFT JOIN creators_table cr_t ON cr_t.id = tcr.creators_id");

                whereClauses.Add(@"(
                    LOWER(t.title_name) LIKE @SearchTerm OR
                    LOWER(cast_t.name) LIKE @SearchTerm OR
                    LOWER(dir_t.name) LIKE @SearchTerm OR
                    LOWER(wri_t.name) LIKE @SearchTerm OR
                    LOWER(cr_t.name) LIKE @SearchTerm
                )");

                parameters.Add("SearchTerm", $"%{filterSettings.SearchTerm.ToLower()}%");
            }
        }

        public static IEnumerable<IdNameItem> GetAllItems(string table)
        {
            var result = new List<IdNameItem>();

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            using (var checkCommand = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName", connection))
            {
                checkCommand.Parameters.AddWithValue("@tableName", table);
                var exists = checkCommand.ExecuteScalar();
                if (exists == null) return result;
            }

            using var command = new SQLiteCommand($"SELECT id, name FROM {table}", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new IdNameItem
                {
                    Id = reader["id"].ToString() ?? "",
                    Name = reader["name"].ToString() ?? ""
                });
            }

            return result;
        }

#region List related

        public static List<(string name, string uuid)> GetListsFromDatabase()
        {
            var lists = new List<(string, string)>();
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            using (var checkCmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='lists_table';", connection))
            {
                using var checkReader = checkCmd.ExecuteReader();
                if (!checkReader.HasRows) return lists;
            }

            using var command = new SQLiteCommand("SELECT name, uuid FROM lists_table;", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lists.Add((reader.GetString(0), reader.GetString(1)));
            }

            return lists;
        }

        public static void AddMovieToList(string listId, string movieId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
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

        public static void RemoveMovieFromList(string listId, string movieId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                string query = @"
                    DELETE FROM list_movies_table 
                    WHERE list_id = @ListId AND movie_id = @MovieId;";

                connection.Execute(query, new { ListId = listId, MovieId = movieId });
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error removing movie from list: {ex.Message}");
            }
        }

        public static bool IsMovieInList(string list_id, string movieId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT COUNT(*) 
                FROM list_movies_table lm
                JOIN lists_table l ON lm.list_id = l.uuid
                WHERE l.uuid = @ListId AND lm.movie_id = @MovieId";

            int count = connection.ExecuteScalar<int>(query, new { ListId = list_id, MovieId = movieId });
            return count > 0;
        }

        public static void CreateListsTable() {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS lists_table (
                    uuid TEXT NOT NULL UNIQUE PRIMARY KEY,
                    name TEXT NOT NULL
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

        public static void CreateCalendarTable()
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS calendar_table (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    date TEXT NOT NULL,
                    title_id TEXT NOT NULL
                );
            ");
        }

        public static void AddMovieToDate(string date, string title_id)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            
            connection.Execute(
                @"INSERT OR IGNORE INTO calendar_table(date, title_id) VALUES(@Date, @Title);",
                    new { Date = date, Title = title_id }
            );
        }

        public static Dictionary<DateTime, List<string>> LoadEntriesForMonth(DateTime start, DateTime end)
        {
            using var conn = new SQLiteConnection(connectionString);
            conn.Open();

            var rows = conn.Query<(string date, string title_id)>(@"
                SELECT DISTINCT date, title_id
                FROM calendar_table
                WHERE date BETWEEN @Start AND @End;
            ", new { Start = start.ToString("yyyy-MM-dd"), End = end.ToString("yyyy-MM-dd") });

            return rows
                .GroupBy(r => DateTime.Parse(r.date))
                .ToDictionary(g => g.Key, g => g.Select(r => r.title_id).ToList());
        }

        public static List<string> CreateNewList() 
        {
            string listName = "My List";
            string uuid = GetNewListUuid();

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            connection.Execute("INSERT INTO lists_table (uuid, name) VALUES (@id, @name)", new {id = uuid, name = listName });

            return [listName, uuid];
        }

        private static string GetNewListUuid()
        {
            return Guid.NewGuid().ToString();
        }

        public static void DeleteList(string listId)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            connection.Execute("DELETE FROM list_movies_table WHERE list_id = @ListId", new { ListId = listId });
            connection.Execute("DELETE FROM lists_table WHERE uuid = @ListId", new { ListId = listId });
        }

        public static void UpdateListName(string oldName, string newName)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "UPDATE lists_table SET name = @newName WHERE name = @oldName";
            connection.Execute(query, new { newName, oldName });
        }

        internal static object GetListUuid(string listName)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "SELECT uuid FROM lists_table WHERE name = @listName";
            var result = connection.ExecuteScalar<string>(query, new { listName });
            return result!;
        }

        internal static string GetListName(string list_id)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "SELECT name FROM lists_table WHERE uuid = @list_id";
            var result = connection.ExecuteScalar<string>(query, new { list_id });
            return result!;
        }

#endregion

#region Title related

        public static async Task<TitleInfo> GetTitleInfo(string id)
        {
            await UpdateTitleInfo(id);

            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = @"SELECT * FROM titles_table WHERE title_id = @id";
            var result = connection.QuerySingleOrDefault<TitleInfo>(query, new { id });

            result.Genres = await GetJoinedNames(connection, "genres_table", "title_genre", "genres_id", id);
            result.Stars = await GetJoinedNames(connection, "cast_table", "title_cast", "cast_id", id);
            result.Writers = await GetJoinedNames(connection, "writers_table", "title_writer", "writers_id", id);
            result.Directors = await GetJoinedNames(connection, "directors_table", "title_director", "directors_id", id);
            result.Creators = await GetJoinedNames(connection, "creators_table", "title_creator", "creators_id", id);
            result.Companies = await GetJoinedNames(connection, "companies_table", "title_company", "companies_id", id);

            return result;
        }

        private static async Task<string?> GetJoinedNames(SQLiteConnection connection, string entityTable, string joinTable, string joinColumn, string titleId)
        {
            string query = $@"
                SELECT e.name 
                FROM {entityTable} e
                JOIN {joinTable} j ON e.id = j.{joinColumn}
                WHERE j.title_id = @titleId";

            var names = (await connection.QueryAsync<string>(query, new { titleId })).ToList();

            return names.Count > 0 ? string.Join(", ", names) : null;
        }

        public static async Task UpdateTitleInfo(string id)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string checkQuery = "SELECT updated FROM titles_table WHERE title_id = @id";
            bool isUpdated = connection.ExecuteScalar<bool>(checkQuery, new { id });

            if (!isUpdated)
            {
                await ServerHandler.ScrapeSingleTitle(id);
            }
        }

        internal static string GetPosterUrl(string id)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "SELECT poster_url FROM titles_table WHERE title_id = @id";
            var result = connection.ExecuteScalar<string>(query, new { id });
            return result!;
        }

        internal static string GetMovieTitle(string id)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "SELECT title_name FROM titles_table WHERE title_id = @id";
            var result = connection.ExecuteScalar<string>(query, new { id });
            return result!;
        }

        internal static string GetSchedule(string id)
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            string query = "SELECT schedule_list FROM titles_table WHERE title_id = @id";
            var result = connection.ExecuteScalar<string>(query, new { id });

            return result!;
        }

        public struct TitleInfo
        {
            public string Title_Id { get; set; }
            public string Title_name { get; set; }
            public string Poster_url { get; set; }
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
            public string? Schedule_list { get; set; }
            public string? Season_count { get; set; }
        }
#endregion

        public class FilterSettings
        {
            public float? MinRating { get; set; } = 0;
            public float? MaxRating { get; set; } = 10;
            public List<string>? Genre { get; set; } = [];
            public int YearStart { get; set; } = 1874;
            public int YearEnd { get; set; } = DateTime.Now.Year + 1;
            public List<string>? Company { get; set; }
            public string? Type { get; set; }
            public string? SearchTerm { get; set; }
            public string? SortBy { get; set; } = "created_on DESC";

            public FilterSettings() { }
        }
    }
}