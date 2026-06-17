using Newtonsoft.Json;
using OnlyR.Core.Models;
using OnlyR.Utils;
using System.Collections.Generic;
using System.IO;

namespace OnlyR.Services.Upload;

public sealed class UploadQueueStore
{
    private readonly string _filePath;
    private readonly List<string> _pendingClusterIds = new();

    public UploadQueueStore()
    {
        _filePath = Path.Combine(OratioPaths.GetStatePath(), "upload_queue.json");
    }

    public int Count => _pendingClusterIds.Count;

    public void Add(ClusterInfo cluster)
    {
        if (!_pendingClusterIds.Contains(cluster.Id))
        {
            _pendingClusterIds.Add(cluster.Id);
            Save();
        }
    }

    public void Remove(string clusterId)
    {
        _pendingClusterIds.Remove(clusterId);
        Save();
    }

    public List<string> Load()
    {
        if (!File.Exists(_filePath))
        {
            return _pendingClusterIds;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var ids = JsonConvert.DeserializeObject<List<string>>(json);
            if (ids != null)
            {
                _pendingClusterIds.Clear();
                _pendingClusterIds.AddRange(ids);
            }
        }
        catch
        {
            _pendingClusterIds.Clear();
        }

        return _pendingClusterIds;
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir != null)
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(_filePath, JsonConvert.SerializeObject(_pendingClusterIds));
    }
}
