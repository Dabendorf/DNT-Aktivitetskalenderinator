using FetchHikes.Constants;
using Serilog;

class Program {
	static async Task Main() {
		try {
			LoggerService.Logger.Information("Application started.");

			const string urlAppendix = "?duration=twothree&associations=25195,24939";
			const string apiUrl = $"{GlobalConstants.urlBase}{urlAppendix}";
			const string dbPath = GlobalConstants.databasePath;

			var dntApiService = new DNTapiService(apiUrl, dbPath);

			var newHikes = await dntApiService.FetchNewHikes();

			if (newHikes.Any()) {
				LoggerService.Logger.Information($"Number of new hikes found: {newHikes.Count}");
				var emailSenderService = new EmailSenderService();

				await emailSenderService.SendEmail(newHikes);
			} else {
				Log.Information("No new hikes found");
			}
		} catch (Exception ex) {
			LoggerService.Logger.Error($"An error occured: {ex.Message}");

			var emailSenderService = new EmailSenderService();
			var errorMessage = $"An error occurred in the FetchHikes application: {ex.Message}\n\n{ex.StackTrace}";
			await emailSenderService.SendErrorEmail(errorMessage);
		}
	}
}