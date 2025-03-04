using FetchHikes.Constants;
using Serilog;

namespace FetchHikes.Services;

public class LoggerService {
	public static Serilog.Core.Logger Logger { get; } = new LoggerConfiguration()
			.WriteTo.Console()
			.WriteTo.File(GlobalConstants.logs, rollingInterval: RollingInterval.Month)
			.CreateLogger();
}