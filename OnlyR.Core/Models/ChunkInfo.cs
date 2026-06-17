using OnlyR.Core.Enums;
using System;

namespace OnlyR.Core.Models;

/// <summary>
/// Metadata for a completed raw recording chunk.
/// </summary>
public sealed class ChunkInfo
{
    public ChunkInfo(string id, string filePath, TrackType trackType, DateTime startTime, DateTime endTime)
    {
        Id = id;
        FilePath = filePath;
        TrackType = trackType;
        StartTime = startTime;
        EndTime = endTime;
    }

    public string Id { get; }
    public string FilePath { get; }
    public TrackType TrackType { get; }
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    public long FileSize { get; set; }
}
