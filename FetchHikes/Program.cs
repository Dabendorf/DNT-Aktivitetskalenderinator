using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MimeKit;
using MailKit.Net.Smtp;

class Program {
	static async Task Main() {
		const string apiUrl = "https://www.dnt.no/api/activities";
		const string dbPath = "hikes.db";

		var newHikes = await FetchNewHikes(apiUrl, dbPath);
		if (newHikes.Any()) {
			await SendEmail(newHikes);
		}
	}

	static async Task<List<Hike>> FetchNewHikes(string apiUrl, string dbPath) {
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
		var email = new MimeMessage();
		email.From.Add(new MailboxAddress("Hike Notifier", "your-email@example.com"));
		email.To.Add(new MailboxAddress("Recipient", "recipient@example.com"));
		email.Subject = "New Hikes Found!";

		var body = "New hikes detected:\n\n" + string.Join("\n", newHikes.Select(h => $"{h.Title} - {h.Url}"));
		email.Body = new TextPart("plain") { Text = body };

		using var smtp = new SmtpClient();
		await smtp.ConnectAsync("smtp.your-email-provider.com", 587, false);
		await smtp.AuthenticateAsync("your-email@example.com", "your-password");
		await smtp.SendAsync(email);
		await smtp.DisconnectAsync(true);
	}
}

record Hike(int Id, string Title, string Url, string PublishDate);
