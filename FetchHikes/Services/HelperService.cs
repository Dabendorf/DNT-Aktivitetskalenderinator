namespace FetchHikes.Services;

public class HelperService() {

	public Dictionary<string, string> ReadCsv(string filePath) {
		var result = new Dictionary<string, string>();

		var lines = File.ReadAllLines(filePath);

		// Skip description
		if (lines.Length == 0) return result;
		var dataLines = lines.Skip(1);

		foreach (var line in dataLines) {
			var parts = line.Split(',', 2);
			if (parts.Length == 2) {
				result[parts[0].Trim('"')] = parts[1].Trim('"');
			}
		}
		return result;
	}
}