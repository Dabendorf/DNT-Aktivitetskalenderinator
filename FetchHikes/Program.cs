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

		const string urlAppendix = "?duration=oversix";
		const string apiUrl = $"{GlobalConstants.urlBase}{urlAppendix}";

		const string dbPath = GlobalConstants.databasePath;

		var newHikes = await FetchNewHikes(apiUrl, dbPath);

		if (newHikes.Any()) {
			Log.Information($"Number of new hikes found: {newHikes.Count}");
			//await SendEmail(newHikes);
		} else {
			Log.Information("No new hikes found");
		}
	}

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
				h.GetProperty("publishDateString").GetProperty("date").GetString() ?? "Unknown Date"
			)).ToList();

		using var connection = new SqliteConnection($"Data Source={dbPath}");
		connection.Open();

		// Create table if not exists
		var createCmd = connection.CreateCommand();
		createCmd.CommandText = "CREATE TABLE IF NOT EXISTS Hikes (id INTEGER PRIMARY KEY, title TEXT, url TEXT, published_date TEXT)";
		createCmd.ExecuteNonQuery();

		Log.Information($"Check for new hikes in database {dbPath}");
		// Check for new hikes
		var newHikes = new List<Hike>();
		foreach (var hike in hikes) {
			var checkCmd = connection.CreateCommand();
			checkCmd.CommandText = "SELECT COUNT(*) FROM Hikes WHERE id = $id";
			checkCmd.Parameters.AddWithValue("$id", hike.Id);

			if ((long)checkCmd.ExecuteScalar() == 0) {
				// Insert new hike
				var insertCmd = connection.CreateCommand();
				insertCmd.CommandText = "INSERT INTO Hikes (id, title, url, published_date) VALUES ($id, $title, $url, $date)";
				insertCmd.Parameters.AddWithValue("$id", hike.Id);
				insertCmd.Parameters.AddWithValue("$title", hike.Title);
				insertCmd.Parameters.AddWithValue("$url", hike.Url);
				insertCmd.Parameters.AddWithValue("$date", hike.PublishDate);
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

			Log.Error("Sent email with new hikes");
		} catch (Exception ex) {
			Log.Error($"Failed to send email: {ex}");
			Console.WriteLine($"Failed to send email: {ex}");
		}
	}
}
