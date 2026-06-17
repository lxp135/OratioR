using OnlyR.Core.Enums;
using OnlyR.Core.Models;
using OnlyR.Utils;
using System;
using System.Globalization;
using System.IO;

namespace OnlyR.Services.Chunking;

/// <summary>
/// Manages raw chunk file paths and metadata.
/// </summary>
public static class ChunkRepository
{
    public static string GenerateFilePath(TrackType trackType, DateTime startTime, int chunkIndex)
    {
        var rawPath = OratioPaths.GetRawPath();
        var timestamp = startTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var typeStr = trackType == TrackType.SystemAudio ? "sys" : "mic";
        var fileName = $"chunk_{timestamp}_{chunkIndex:D4}_{typeStr}.wav";
        return Path.Combine(rawPath, fileName);
    }

    public static ChunkInfo CreateChunkInfo(TrackType trackType, string filePath, DateTime startTime, DateTime endTime)
    {
        var chunkId = Path.GetFileNameWithoutExtension(filePath);
        var info = new ChunkInfo(chunkId, filePath, trackType, startTime, endTime);

        if (File.Exists(filePath))
        {
            info.FileSize = new FileInfo(filePath).Length;
        }

        return info;
    }
}
