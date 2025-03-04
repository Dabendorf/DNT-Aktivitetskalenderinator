using System.Text.Json;
using FetchHikes.Dtos;
using Microsoft.Data.Sqlite;

namespace FetchHikes.Services;

public class DNTapiService(string apiUrl, string dbPath) {
	private readonly string _apiUrl = apiUrl;
	private readonly string _dbPath = dbPath;

	private static string GetPropertyValue(JsonElement element, string propertyName, string fallback = "Unknown") =>
				element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? fallback : fallback;

	public async Task<List<Hike>> FetchNewHikes() {
		LoggerService.Logger.Information($"Fetching new hikes from API {_apiUrl}");

		using var client = new HttpClient();
		var response = await client.GetStringAsync(_apiUrl);
		var jsonDoc = JsonDocument.Parse(response);

		var hikes = jsonDoc.RootElement.GetProperty("pageHits")
			.EnumerateArray()
			.Select(h => new Hike(
				h.GetProperty("id").GetInt32(),
				h.GetProperty("pageTitle").GetString() ?? "No Title",
				h.GetProperty("url").GetString() ?? "",
				GetPropertyValue(h.GetProperty("publishDateString"), "date"),
				h.GetProperty("level").GetString() ?? "Unknown",
				h.GetProperty("organizorName").GetString() ?? "Unknown",
				GetPropertyValue(h.GetProperty("activityViewModel"), "eventLocation"),
				GetPropertyValue(h.GetProperty("activityViewModel"), "mainType"),
				GetPropertyValue(h.GetProperty("activityViewModel"), "targetGroups"),
				GetPropertyValue(h.GetProperty("activityViewModel"), "duration"),
				GetPropertyValue(h.GetProperty("activityViewModel"), "start"),
				GetPropertyValue(h.GetProperty("activityViewModel"), "end"),
				$"{GetPropertyValue(h.GetProperty("activityViewModel"), "startDate")} {GetPropertyValue(h.GetProperty("activityViewModel"), "startTime")}",
				$"{GetPropertyValue(h.GetProperty("activityViewModel"), "endDate")} {GetPropertyValue(h.GetProperty("activityViewModel"), "endTime")}",
				GetPropertyValue(h.GetProperty("activityViewModel"), "registrationStart")
			)).ToList();

		using var connection = new SqliteConnection($"Data Source={_dbPath}");
		connection.Open();

		// Create table if not exists
		var createCmd = connection.CreateCommand();
		createCmd.CommandText = @"
			CREATE TABLE IF NOT EXISTS Hikes (
				id INTEGER PRIMARY KEY,
				title TEXT,
				url TEXT,
				published_date TEXT,
				event_location TEXT,
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

		LoggerService.Logger.Information($"Check for new hikes in database {_dbPath}");
		// Check for new hikes
		var newHikes = new List<Hike>();
		foreach (var hike in hikes) {
			var checkCmd = connection.CreateCommand();
			checkCmd.CommandText = "SELECT COUNT(*) FROM Hikes WHERE id = $id";
			checkCmd.Parameters.AddWithValue("$id", hike.Id);

			var result = checkCmd.ExecuteScalar();
			var count = result == DBNull.Value ? 0 : Convert.ToInt64(result); // Handle null safely

			if (count == 0) {
				var insertCmd = connection.CreateCommand();
				insertCmd.CommandText = @"
					INSERT INTO Hikes (
						id, title, url, published_date, event_location, main_type, target_groups, 
						duration, start, end, start_readable, end_readable, registration_start
					) VALUES (
						$id, $title, $url, $publishedDate, $eventLocation, $mainType, $targetGroups, 
						$duration, $start, $end, $startReadable, $endReadable, $registrationStart
					)";

				insertCmd.Parameters.AddWithValue("$id", hike.Id);
				insertCmd.Parameters.AddWithValue("$title", hike.Title);
				insertCmd.Parameters.AddWithValue("$url", hike.Url);
				//insertCmd.Parameters.AddWithValue("$url", $"https://www.dnt.no/api/search/activitydetails?id={hike.Id}");
				insertCmd.Parameters.AddWithValue("$publishedDate", hike.PublishDate);
				insertCmd.Parameters.AddWithValue("$eventLocation", hike.EventLocation);
				insertCmd.Parameters.AddWithValue("$mainType", hike.MainType);
				insertCmd.Parameters.AddWithValue("$targetGroups", hike.TargetGroups);
				insertCmd.Parameters.AddWithValue("$duration", hike.Duration);
				insertCmd.Parameters.AddWithValue("$start", hike.Start);  // Treat as TEXT
				insertCmd.Parameters.AddWithValue("$end", hike.End);      // Treat as TEXT
				insertCmd.Parameters.AddWithValue("$startReadable", hike.StartReadable);
				insertCmd.Parameters.AddWithValue("$endReadable", hike.EndReadable);
				insertCmd.Parameters.AddWithValue("$registrationStart", hike.RegistrationStart); // Treat as TEXT

				insertCmd.ExecuteNonQuery();

				newHikes.Add(hike);
			}
		}

		return newHikes;
	}
}