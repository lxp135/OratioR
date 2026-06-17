using Serilog;
using System.Globalization;
using System.IO;

namespace OnlyR.Utils;

/// <summary>
/// Oratio 日志配置。从 App.xaml.cs 抽取。
/// </summary>
public static class LogConfig
{
    public static void Initialize()
    {
        var logsDirectory = OratioPaths.GetLogsPath();
        Directory.CreateDirectory(logsDirectory);

#if DEBUG
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logsDirectory, "log-.txt"),
                retainedFileCountLimit: 28,
                rollingInterval: RollingInterval.Day,
                formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
#else
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logsDirectory, "log-.txt"),
                retainedFileCountLimit: 28,
                rollingInterval: RollingInterval.Day,
                formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
#endif

        Log.Logger.Information("==== Oratio Launched ====");
    }
}
