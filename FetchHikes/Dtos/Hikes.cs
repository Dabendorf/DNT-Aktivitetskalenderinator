namespace FetchHikes.Dtos;

public record Hike(
	int Id,
	string Title,
	string Url,
	string PublishDate,
	string Level,
	string OrganisorName,
	string EventLocation,
	string SearchQuery,
	string MainType,
	string TargetGroups,
	string Duration,
	string Start,
	string End,
	string StartReadable,
	string EndReadable,
	string RegistrationStart
);
