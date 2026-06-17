using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace OnlyR.Utils;

public static class FfmpegHelper
{
    public static bool IsFfmpegAvailable(string? customPath = null)
    {
        var candidates = new List<string>();

        if (customPath != null)
        {
            candidates.Add(customPath);
        }

        candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"));
        candidates.Add("ffmpeg.exe");

        foreach (var path in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit(3000);
                if (process?.ExitCode == 0)
                {
                    return true;
                }
            }
            catch
            {
                // try next
            }
        }

        return false;
    }

    public static string ResolveFfmpegPath(string? customPath = null)
    {
        if (customPath != null && File.Exists(customPath))
        {
            return customPath;
        }

        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var appPath = Path.Combine(appDir, "ffmpeg.exe");
        if (File.Exists(appPath))
        {
            return appPath;
        }

        return "ffmpeg.exe";
    }

    public static string BuildMergeCommand(string sysFile, string micFile, string outputFile)
    {
        return $"-i \"{sysFile}\" -i \"{micFile}\" -filter_complex \"[0:a][1:a]join=inputs=2:channel_layout=stereo\" -c:a libmp3lame -q:a 5 \"{outputFile}\" -y";
    }
}
