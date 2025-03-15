using System.Text.Json;
using DNTkalenderinator.Dtos;

namespace DNTkalenderinator.Services;

public class DNTapiService() {
	private static string GetPropertyValue(JsonElement element, string propertyName, string fallback = "Unknown") =>
				element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? fallback : fallback;

	public async Task<List<Hike>> GetHikesFromApi(string apiUrl, string searchQuery) {
		LoggerService.Logger.Information($"Fetching new hikes from API {apiUrl}");

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
				searchQuery,
				GetPropertyValue(h.GetProperty("activityViewModel"), "mainType"),
				GetPropertyValue(h.GetProperty("activityViewModel"), "targetGroups"),
				GetPropertyValue(h.GetProperty("activityViewModel"), "duration"),
				GetPropertyValue(h.GetProperty("activityViewModel"), "start"),
				GetPropertyValue(h.GetProperty("activityViewModel"), "end"),
				$"{GetPropertyValue(h.GetProperty("activityViewModel"), "startDate")} {GetPropertyValue(h.GetProperty("activityViewModel"), "startTime")}",
				$"{GetPropertyValue(h.GetProperty("activityViewModel"), "endDate")} {GetPropertyValue(h.GetProperty("activityViewModel"), "endTime")}",
				GetPropertyValue(h.GetProperty("activityViewModel"), "registrationStart")
			)).Where(h => DateTimeOffset.TryParse(h.Start, out var startTime) && startTime > TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Europe/Oslo")).ToList();

		return hikes;
	}
}