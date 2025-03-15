using DNTkalenderinator.Dtos;
using Microsoft.Data.Sqlite;

namespace DNTkalenderinator.Services;

public class DatabaseService() {
	public async Task DeletePastHikes(string dbPath) {
		using var connection = new SqliteConnection($"Data Source={dbPath}");
		await connection.OpenAsync();

		// Check if the Hikes table exists
		var checkCmd = connection.CreateCommand();
		checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Hikes'";
		var tableExists = (await checkCmd.ExecuteScalarAsync()) as long? ?? 0;

		if (tableExists == 0) {
			LoggerService.Logger.Information($"Table 'Hikes' does not exist in database {dbPath}. Skipping deletion.");
			return;
		}

		var nowUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK"); // Current UTC timestamp

		var deleteCmd = connection.CreateCommand();
		deleteCmd.CommandText = "DELETE FROM Hikes WHERE start < $now";
		deleteCmd.Parameters.AddWithValue("$now", nowUtc);

		int rowsDeleted = await deleteCmd.ExecuteNonQueryAsync();
		LoggerService.Logger.Information($"Deleted {rowsDeleted} past hikes from database {dbPath}");
	}

	public async Task<List<Hike>> CompareWithDatabase(List<Hike> hikes, string dbPath) {
		using var connection = new SqliteConnection($"Data Source={dbPath}");
		await connection.OpenAsync();

		// Create table if not exists
		var createCmd = connection.CreateCommand();
		createCmd.CommandText = @"
			CREATE TABLE IF NOT EXISTS Hikes (
				id INTEGER PRIMARY KEY,
				title TEXT,
				url TEXT,
				published_date TEXT,
				event_location TEXT,
				search_query TEXT,
				main_type TEXT,
				target_groups TEXT,
				duration TEXT,
				start TEXT,
				end TEXT,
				start_readable TEXT,
				end_readable TEXT,
				registration_start TEXT
			)";
		createCmd.ExecuteNonQuery();

		LoggerService.Logger.Information($"Check for new hikes in database {dbPath}");

		// Check for new hikes
		var newHikes = new List<Hike>();
		foreach (var hike in hikes) {
			var checkCmd = connection.CreateCommand();
			checkCmd.CommandText = "SELECT COUNT(*) FROM Hikes WHERE id = $id";
			checkCmd.Parameters.AddWithValue("$id", hike.Id);

			var result = await checkCmd.ExecuteScalarAsync();
			var count = result == DBNull.Value ? 0 : Convert.ToInt64(result); // Handle null safely

			if (count == 0) {
				var insertCmd = connection.CreateCommand();
				insertCmd.CommandText = @"
					INSERT INTO Hikes (
						id, title, url, published_date, event_location, search_query, main_type, target_groups, 
						duration, start, end, start_readable, end_readable, registration_start
					) VALUES (
						$id, $title, $url, $publishedDate, $eventLocation, $searchQuery, $mainType, $targetGroups, 
						$duration, $start, $end, $startReadable, $endReadable, $registrationStart
					)";

				insertCmd.Parameters.AddWithValue("$id", hike.Id);
				insertCmd.Parameters.AddWithValue("$title", hike.Title);
				insertCmd.Parameters.AddWithValue("$url", hike.Url);
				//insertCmd.Parameters.AddWithValue("$url", $"https://www.dnt.no/api/search/activitydetails?id={hike.Id}");
				insertCmd.Parameters.AddWithValue("$publishedDate", hike.PublishDate);
				insertCmd.Parameters.AddWithValue("$eventLocation", hike.EventLocation);
				insertCmd.Parameters.AddWithValue("$searchQuery", hike.SearchQuery);
				insertCmd.Parameters.AddWithValue("$mainType", hike.MainType);
				insertCmd.Parameters.AddWithValue("$targetGroups", hike.TargetGroups);
				insertCmd.Parameters.AddWithValue("$duration", hike.Duration);
				insertCmd.Parameters.AddWithValue("$start", hike.Start);  // Treat as TEXT
				insertCmd.Parameters.AddWithValue("$end", hike.End);      // Treat as TEXT
				insertCmd.Parameters.AddWithValue("$startReadable", hike.StartReadable);
				insertCmd.Parameters.AddWithValue("$endReadable", hike.EndReadable);
				insertCmd.Parameters.AddWithValue("$registrationStart", hike.RegistrationStart); // Treat as TEXT

				await insertCmd.ExecuteNonQueryAsync();

				newHikes.Add(hike);
			}
		}

		return newHikes;
	}
}