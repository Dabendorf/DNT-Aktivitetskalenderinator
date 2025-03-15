using DNTkalenderinator.Constants;
using DNTkalenderinator.Dtos;
using Serilog;
using DNTkalenderinator.Services;

class Program {
	static async Task Main() {
		try {
			LoggerService.Logger.Information("Application started.");

			const string dbPath = GlobalConstants.databasePath;

			var dntApiService = new DNTapiService();
			var databaseService = new DatabaseService();
			var helperService = new HelperService();

			Dictionary<string, string> searchQueries = helperService.ReadCsv(GlobalConstants.queryFilePath);

			// if one object is called [ALL], it is a superfilter for all queries
			var superFilter = searchQueries.TryGetValue("[ALL]", out var valueInclude) ? valueInclude : "";
			searchQueries.Remove("[ALL]");

			// elements inside [EXCLUDE] (comma separated) are searched inside the title, if in a title, its skipped
			var superExcluder = searchQueries.TryGetValue("[EXCLUDE]", out var valueExclude) ? valueExclude.Split(',') : Array.Empty<string>();
			searchQueries.Remove("[EXCLUDE]");

			var newHikes = new List<Hike>();
			await databaseService.DeletePastHikes(dbPath);

			foreach (var (description, searchQuery) in searchQueries) {
				var apiUrl = $"{GlobalConstants.urlBase}?pageSize=1000{superFilter}{searchQuery}";
				var hikesInApi = await dntApiService.GetHikesFromApi(apiUrl, description);

				var newHikesTemp = await databaseService.CompareWithDatabase(hikesInApi, dbPath);

				if (newHikesTemp != null) {
					foreach (var hike in newHikesTemp) {
						// Check if the title contains any word in the exclusion list
						if (superExcluder.Any(excludeWord => hike.Title.Contains(excludeWord, StringComparison.OrdinalIgnoreCase))) {
							continue; // Skip adding this hike
						}

						newHikes.Add(hike);
					}
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
			var errorMessage = $"An error occurred in the DNTkalenderinator application: {ex.Message}\n\n{ex.StackTrace}";
			await emailSenderService.SendErrorEmail(errorMessage);
		}
	}
}