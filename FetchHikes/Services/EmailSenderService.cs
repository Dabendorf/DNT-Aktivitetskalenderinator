using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Text.Json;
using FetchHikes.Dtos;
using FetchHikes.Constants;
using System.Text;

namespace FetchHikes.Services;

public class EmailSenderService {
	public async Task SendEmail(List<Hike> newHikes) {
		try {
			var secretsJson = await File.ReadAllTextAsync(GlobalConstants.secretsPath);
			var emailSettings = JsonSerializer.Deserialize<EmailSettings>(secretsJson);

			if (emailSettings == null) {
				LoggerService.Logger.Error($"Failed to send email, could not read secrets.json");
				return;
			}

			var email = new MimeMessage();
			email.From.Add(new MailboxAddress(emailSettings.FromName, emailSettings.FromEmail));
			email.To.Add(new MailboxAddress(emailSettings.ToName, emailSettings.ToEmail));
			email.Subject = emailSettings.Subject;

			var body = GenerateBody(newHikes);
			email.Body = new TextPart("html") { Text = body };

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
		}
	}

	private string GenerateBody(List<Hike> newHikes) {
		var tableRows = new StringBuilder();

		var orderedHikes = newHikes.OrderBy(hike => hike.Start).ToList();

		foreach (var hike in orderedHikes) {
			tableRows.AppendLine("<tr>");
			tableRows.AppendLine($"<td>{hike.Title}</td>");
			tableRows.AppendLine($"<td>{hike.Duration}</td>");
			tableRows.AppendLine($"<td>{hike.StartReadable}</td>");
			tableRows.AppendLine($"<td>{hike.EndReadable}</td>");
			tableRows.AppendLine($"<td><a href=\"{$"https://www.dnt.no/api/search/activitydetails?id={hike.Id}"}\">Link</a></td>");
			tableRows.AppendLine($"<td>{hike.Level}</td>");
			tableRows.AppendLine($"<td>{hike.OrganisorName}</td>");
			tableRows.AppendLine($"<td>{hike.EventLocation}</td>");
			tableRows.AppendLine($"<td>{hike.SearchQuery}</td>");
			tableRows.AppendLine($"<td>{hike.MainType}</td>");
			tableRows.AppendLine($"<td>{hike.TargetGroups}</td>");
			tableRows.AppendLine($"<td>{hike.PublishDate}</td>");
			tableRows.AppendLine($"<td>{hike.RegistrationStart}</td>");
			tableRows.AppendLine("</tr>");
		}

		var body = $@"
            <html>
                <body>
                    <h2>New Hikes Detected</h2>
                    <table border='1' style='border-collapse: collapse;'>
                        <tr>
                            <th>Title</th>
                            <th>Duration</th>
                            <th>Start</th>
                            <th>End</th>
                            <th>URL</th>
                            <th>Level</th>
                            <th>Organisor</th>
                            <th>Location</th>
							<th>SearchQuery</th>
                            <th>Main Type</th>
                            <th>Target Groups</th>
							<th>Publish Date</th>
                            <th>Registration Start</th>
                        </tr>
                        {tableRows}
                    </table>
                </body>
            </html>";

		return body;
	}

	public async Task SendErrorEmail(string errorMessage) {
		try {
			var secretsJson = await File.ReadAllTextAsync(GlobalConstants.secretsPath);
			var emailSettings = JsonSerializer.Deserialize<EmailSettings>(secretsJson);

			if (emailSettings == null) {
				LoggerService.Logger.Error($"Failed to send error email, could not read {GlobalConstants.secretsPath}");
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
		}
	}
}