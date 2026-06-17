using System;
using System.IO;

namespace OnlyR.Utils;

/// <summary>
/// Oratio 数据目录路径常量。所有录音和状态文件位于 %LOCALAPPDATA%\Oratio\ 下。
/// </summary>
public static class OratioPaths
{
    private static readonly string BaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Oratio");

    private static readonly string RecordingsDir = Path.Combine(BaseDir, "recordings");

    public static string GetRawPath() =>
        Path.Combine(RecordingsDir, "raw");

    public static string GetProcessedPath() =>
        Path.Combine(RecordingsDir, "processed");

    public static string GetStatePath() =>
        Path.Combine(RecordingsDir, "state");

    public static string GetLogsPath() =>
        Path.Combine(BaseDir, "logs");

    public static string GetBasePath() => BaseDir;

    public static void EnsureAll()
    {
        Directory.CreateDirectory(GetRawPath());
        Directory.CreateDirectory(GetProcessedPath());
        Directory.CreateDirectory(GetStatePath());
        Directory.CreateDirectory(GetLogsPath());
    }
}
