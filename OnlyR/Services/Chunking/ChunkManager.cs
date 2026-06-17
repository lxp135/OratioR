using OnlyR.Core.Enums;
using OnlyR.Services.Audio;
using Serilog;
using System;
using System.Globalization;
using System.Timers;
using Timer = System.Timers.Timer;

namespace OnlyR.Services.Chunking;

/// <summary>
/// Manages periodic recording chunk rotation.
/// </summary>
public sealed class ChunkManager : IDisposable
{
    private readonly IAudioCaptureService _captureService;
    private readonly int _chunkDurationMinutes;
    private Timer? _timer;
    private DateTime _chunkStartTime;
    private int _chunkIndex;

    public ChunkManager(IAudioCaptureService captureService, int chunkDurationMinutes = 30)
    {
        _captureService = captureService;
        _chunkDurationMinutes = chunkDurationMinutes;
    }

    public event EventHandler? ChunkCreationRequested;

    public void Start()
    {
        _chunkStartTime = DateTime.UtcNow;
        _chunkIndex = 0;

        _timer = new Timer(TimeSpan.FromMinutes(_chunkDurationMinutes));
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();

        Log.Logger.Information("ChunkManager started, interval={Interval}min", _chunkDurationMinutes);
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        Log.Logger.Information("ChunkManager stopped");
    }

    public void FlushCurrentChunk()
    {
        Log.Logger.Information("ChunkManager flushing current chunk");
        _timer?.Stop();
    }

    public string GenerateChunkFileName(TrackType trackType)
    {
        var timestamp = _chunkStartTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var typeStr = trackType == TrackType.SystemAudio ? "sys" : "mic";
        return $"chunk_{timestamp}_{_chunkIndex:D4}_{typeStr}.mp3";
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _chunkIndex++;
        _chunkStartTime = DateTime.UtcNow;
        ChunkCreationRequested?.Invoke(this, EventArgs.Empty);
        Log.Logger.Information("Chunk creation requested, index={Index}", _chunkIndex);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
