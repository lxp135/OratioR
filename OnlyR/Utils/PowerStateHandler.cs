using OnlyR.Services.Chunking;
using Microsoft.Win32;
using Serilog;
using System;

namespace OnlyR.Utils;

/// <summary>
/// Listens for system sleep/wake events and flushes/resumes recording.
/// </summary>
public sealed class PowerStateHandler : IDisposable
{
    private readonly ChunkManager _chunkManager;
    private DateTime _lastSuspendTime;

    public PowerStateHandler(ChunkManager chunkManager)
    {
        _chunkManager = chunkManager;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    public DateTime LastSuspendTime => _lastSuspendTime;
    public TimeSpan? SleepDuration { get; private set; }

    public event EventHandler? SuspendDetected;
    public event EventHandler? ResumeDetected;

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            _lastSuspendTime = DateTime.UtcNow;
            _chunkManager.FlushCurrentChunk();
            SuspendDetected?.Invoke(this, EventArgs.Empty);
            Log.Logger.Information("System suspending");
        }
        else if (e.Mode == PowerModes.Resume)
        {
            var now = DateTime.UtcNow;
            SleepDuration = now - _lastSuspendTime;
            ResumeDetected?.Invoke(this, EventArgs.Empty);
            Log.Logger.Information("System resumed, sleep duration={Duration}", SleepDuration);
        }
    }

    public TimeSpan GetSleepDuration()
    {
        return SleepDuration ?? TimeSpan.Zero;
    }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        GC.SuppressFinalize(this);
    }
}
