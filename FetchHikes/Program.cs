using FetchHikes.Constants;
using FetchHikes.Dtos;
using Serilog;
using FetchHikes.Services;

class Program {
	static async Task Main() {
		try {
			LoggerService.Logger.Information("Application started.");

			const string dbPath = GlobalConstants.databasePath;

			var dntApiService = new DNTapiService();
			var databaseService = new DatabaseService();
			var helperService = new HelperService();

			Dictionary<string, string> searchQueries = helperService.ReadCsv(GlobalConstants.queryFilePath);

			var newHikes = new List<Hike>();
			await databaseService.DeletePastHikes(dbPath);

			foreach (var (description, searchQuery) in searchQueries) {
				var apiUrl = $"{GlobalConstants.urlBase}?pageSize=1000&{searchQuery}";
				var hikesInApi = await dntApiService.GetHikesFromApi(apiUrl, description);

				var newHikesTemp = await databaseService.CompareWithDatabase(hikesInApi, dbPath);

				if (newHikesTemp != null) {
					newHikes.AddRange(newHikesTemp);
				}
			}

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