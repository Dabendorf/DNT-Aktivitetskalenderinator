using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Text.Json;
using FetchHikes.Dtos;
using FetchHikes.Constants;

public class EmailSenderService {
	public async Task SendEmail(List<Hike> newHikes) {
		try {
			var secretsJson = await File.ReadAllTextAsync(GlobalConstants.secretsPath);
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

			LoggerService.Logger.Information("Sent email with new hikes");
		} catch (Exception ex) {
			LoggerService.Logger.Error($"Failed to send email: {ex}");
			Console.WriteLine($"Failed to send email: {ex}");
		}
	}

	public async Task SendErrorEmail(string errorMessage) {
		try {
			var secretsJson = await File.ReadAllTextAsync(GlobalConstants.secretsPath);
			var emailSettings = JsonSerializer.Deserialize<EmailSettings>(secretsJson);

			if (emailSettings == null) {
				Console.WriteLine($"Failed to send error email, could not read {GlobalConstants.secretsPath}");
				return;
			}

			var email = new MimeMessage();
			email.From.Add(new MailboxAddress(emailSettings.FromName, emailSettings.FromEmail));
			email.To.Add(new MailboxAddress(emailSettings.ToName, emailSettings.ToEmail));
			email.Subject = "Error in DNT Calendar Fetcher Application";

			var body = $"An error occurred in the DNT Calendar Fetcher Application:\n\n{errorMessage}";
			email.Body = new TextPart("plain") { Text = body };

			using var smtp = new SmtpClient();
			smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
			await smtp.ConnectAsync(emailSettings.SmtpServer, emailSettings.SmtpPort, SecureSocketOptions.StartTls);
			await smtp.AuthenticateAsync(emailSettings.Username, emailSettings.Password);
			await smtp.SendAsync(email);
			await smtp.DisconnectAsync(true);

			LoggerService.Logger.Information("Sent error email.");
		} catch (Exception ex) {
			LoggerService.Logger.Error($"Failed to send error email: {ex}");
			Console.WriteLine($"Failed to send error email: {ex}");
		}
	}
}