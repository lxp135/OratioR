using OnlyR.Utils;
using Serilog;
using System;

namespace OnlyR.Services.Cleanup;

/// <summary>
/// Deletes files older than the configured retention period on startup.
/// </summary>
public sealed class CleanupService
{
    private readonly int _retentionDays;

    public CleanupService(int retentionDays = 7)
    {
        _retentionDays = retentionDays;
    }

    public int Execute()
    {
        var count = 0;
        var dirs = new[] { OratioPaths.GetRawPath(), OratioPaths.GetProcessedPath(), OratioPaths.GetStatePath() };

        foreach (var dir in dirs)
        {
            var deleted = FileUtils.DeleteFilesOlderThan(dir, _retentionDays);
            count += deleted;
        }

        Log.Logger.Information("Cleanup completed: {Count} files removed", count);
        return count;
    }
}
