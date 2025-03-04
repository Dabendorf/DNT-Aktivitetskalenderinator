using System.Text.Json;
using Microsoft.Data.Sqlite;
using MimeKit;
using MailKit.Net.Smtp;
using FetchHikes.Constants;
using FetchHikes.Dtos;
using MailKit.Security;
using Serilog;
using Serilog.Core;

class Program {
	static async Task Main() {
		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console()
			.WriteTo.File(GlobalConstants.logs, rollingInterval: RollingInterval.Month)
			.CreateLogger();

		const string urlAppendix = "?duration=twothree&associations=25195,24939";
		const string apiUrl = $"{GlobalConstants.urlBase}{urlAppendix}";

		const string dbPath = GlobalConstants.databasePath;

		var newHikes = await FetchNewHikes(apiUrl, dbPath);

		if (newHikes.Any()) {
			Log.Information($"Number of new hikes found: {newHikes.Count}");
			await SendEmail(newHikes);
		} else {
			Log.Information("No new hikes found");
		}
	}

	static string GetPropertyValue(JsonElement element, string propertyName, string fallback = "Unknown") =>
			element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? fallback : fallback;

	static async Task<List<Hike>> FetchNewHikes(string apiUrl, string dbPath) {
		Log.Information($"Fetching new hikes from API {apiUrl}");

		using var client = new HttpClient();
		var response = await client.GetStringAsync(apiUrl);
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

		using var connection = new SqliteConnection($"Data Source={dbPath}");
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

		Log.Information($"Check for new hikes in database {dbPath}");
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

	static async Task SendEmail(List<Hike> newHikes) {
		try {
			var secretsJson = await File.ReadAllTextAsync("secrets.json");
			var emailSettings = JsonSerializer.Deserialize<EmailSettings>(secretsJson);

			if (emailSettings == null) {
				Console.WriteLine($"Failed to send email, could not read secrets.json");
				return;
			}

			var email = new MimeMessage();
			email.From.Add(new MailboxAddress(emailSettings.FromName, emailSettings.FromEmail));
			email.To.Add(new MailboxAddress(emailSettings.ToName, emailSettings.ToEmail));
			email.Subject = emailSettings.Subject;

			var body = "New hikes detected:\n\n" + string.Join("\n", newHikes.Select(h => $"{h.Title} - {h.Url}"));
			email.Body = new TextPart("plain") { Text = body };

			using var smtp = new SmtpClient();
			smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
			//await smtp.ConnectAsync(emailSettings.SmtpServer, emailSettings.SmtpPort, emailSettings.UseSsl);
			await smtp.ConnectAsync(emailSettings.SmtpServer, emailSettings.SmtpPort, SecureSocketOptions.StartTls);
			await smtp.AuthenticateAsync(emailSettings.Username, emailSettings.Password);
			await smtp.SendAsync(email);
			await smtp.DisconnectAsync(true);

			Log.Information("Sent email with new hikes");
		} catch (Exception ex) {
			Log.Error($"Failed to send email: {ex}");
			Console.WriteLine($"Failed to send email: {ex}");
		}
	}
}
