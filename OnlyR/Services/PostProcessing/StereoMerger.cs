using OnlyR.Utils;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;

namespace OnlyR.Services.PostProcessing;

public sealed class StereoMerger
{
    private readonly string? _ffmpegPath;

    public StereoMerger(string? ffmpegPath = null)
    {
        _ffmpegPath = ffmpegPath;
    }

    public bool Merge(string sysFile, string micFile, string outputFile)
    {
        var args = FfmpegHelper.BuildMergeCommand(sysFile, micFile, outputFile);
        var path = FfmpegHelper.ResolveFfmpegPath(_ffmpegPath);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = args,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            process.WaitForExit(30000);

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                Log.Logger.Error("ffmpeg merge failed: {Error}", error);
                return false;
            }

            return File.Exists(outputFile) && new FileInfo(outputFile).Length > 0;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "StereoMerger.Merge exception");
            return false;
        }
    }
}
