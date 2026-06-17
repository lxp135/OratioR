using OnlyR.Core.Models;
using OnlyR.Utils;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OnlyR.Services.Recovery;

/// <summary>
/// Scans residual files from previous sessions and rebuilds upload queue.
/// </summary>
public sealed class SessionRecoveryService
{
    private readonly Upload.UploadQueueStore _queueStore;

    public SessionRecoveryService(Upload.UploadQueueStore queueStore)
    {
        _queueStore = queueStore;
    }

    public List<string> ScanProcessedFiles()
    {
        var processedPath = OratioPaths.GetProcessedPath();
        if (!Directory.Exists(processedPath))
        {
            return new List<string>();
        }

        var mp3Files = Directory.EnumerateFiles(processedPath, "cluster_*.mp3").ToList();
        Log.Logger.Information("Session recovery: found {Count} processed files", mp3Files.Count);

        foreach (var file in mp3Files)
        {
            var clusterId = Path.GetFileNameWithoutExtension(file).Replace("cluster_", "");
            _queueStore.Add(new ClusterInfo { Id = clusterId, FilePath = file, State = Core.Enums.ClusterState.Closed });
        }

        return mp3Files;
    }
}
