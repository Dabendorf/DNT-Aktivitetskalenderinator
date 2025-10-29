using DNTkalenderinator.Constants;
using DNTkalenderinator.Dtos;
using Serilog;
using DNTkalenderinator.Services;

class Program {
	static async Task Main(string[] args) {
		try {
			LoggerService.Logger.Information("Application started.");

			const string dbPath = GlobalConstants.databasePath;

			var dntApiService = new DNTapiService();
			var databaseService = new DatabaseService();
			var helperService = new HelperService();

			// A list of search queries
			Dictionary<string, string> searchQueries = helperService.ReadCsv(GlobalConstants.queryFilePath);

			// if one object is called [ALL], it is a superfilter for all queries
			var superFilter = searchQueries.TryGetValue("[ALL]", out var valueInclude) ? valueInclude : "";
			searchQueries.Remove("[ALL]");

			// elements inside [EXCLUDE] (comma separated) are searched inside the title, if in a title, its skipped
			var superExcluder = searchQueries.TryGetValue("[EXCLUDE]", out var valueExclude) ? valueExclude.Split(',') : [];
			searchQueries.Remove("[EXCLUDE]");

			// Delete all hikes which are in the past
			await databaseService.DeletePastHikes(dbPath);

			var newHikes = new List<Hike>();
			
			// Since the programme sends an email every week and you may want to keep them for the summer, you get a lot of emails
			// if you pass the argument --summary to it, you get a new email with all previously sent hikes which are still happening
			var summaryEmail = args.Contains("--summary", StringComparer.OrdinalIgnoreCase);

			if(summaryEmail) {
				var newHikesTemp = await databaseService.GetCurrentHikeTable(dbPath);
				newHikes.AddRange(newHikes);

				if (newHikesTemp != null) {
					newHikes.AddRange(newHikesTemp);
				}
			} else {
				// Loop through all search queries, running API calls
				foreach (var (description, searchQuery) in searchQueries) {
					// If description contains IGNOREALL (case insensitive), do not apply superFilter
					var filter = description.Contains("IGNOREALL", StringComparison.OrdinalIgnoreCase)? "": superFilter;

					var apiUrl = $"{GlobalConstants.urlBase}?pageSize=1000{filter}{searchQuery}";
					var hikesInApi = await dntApiService.GetHikesFromApi(apiUrl, description.Replace("IGNOREALL","(kein All)"));

					// Compare with database, only returns things not being in the database yet
					var filteredHikes = hikesInApi
						.Where(hike => !superExcluder.Any(excludeWord => hike.Title.Contains(excludeWord, StringComparison.OrdinalIgnoreCase)))
						.ToList();

					var newHikesTemp = await databaseService.CompareWithDatabase(filteredHikes, dbPath);

					if (newHikesTemp != null) {
						newHikes.AddRange(newHikesTemp);
					}
				}
			}

			// Send Email if new hikes are found
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