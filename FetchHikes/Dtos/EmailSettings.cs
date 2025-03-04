namespace FetchHikes.Dtos;

public class EmailSettings {
	public required string SmtpServer { get; set; }
	public required int SmtpPort { get; set; }
	public required bool UseSsl { get; set; }
	public required string Username { get; set; }
	public required string Password { get; set; }
	public required string FromName { get; set; }
	public required string FromEmail { get; set; }
	public required string ToName { get; set; }
	public required string ToEmail { get; set; }
	public required string Subject { get; set; }
}
