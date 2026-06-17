using OnlyR.Utils;
using Serilog;
using System;
using System.Timers;

namespace OnlyR.Services.Cleanup;

/// <summary>
/// Monitors disk space at the recording directory. Stops recording if below threshold.
/// </summary>
public sealed class DiskSpaceMonitor : IDisposable
{
    private readonly Timer _timer;
    private readonly long _thresholdBytes;

    public DiskSpaceMonitor(int checkIntervalMinutes = 5, long thresholdMb = 500)
    {
        _thresholdBytes = thresholdMb * 1024 * 1024;
        _timer = new Timer(TimeSpan.FromMinutes(checkIntervalMinutes));
        _timer.Elapsed += OnCheck;
        _timer.AutoReset = true;
    }

    public event EventHandler<bool>? DiskSpaceLow;

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void OnCheck(object? sender, ElapsedEventArgs e)
    {
        var rawPath = OratioPaths.GetRawPath();
        if (FileUtils.DriveFreeBytes(rawPath, out var freeBytes))
        {
            var isLow = freeBytes < (ulong)_thresholdBytes;
            if (isLow)
            {
                Log.Logger.Warning("Disk space low: {FreeBytes} bytes free", freeBytes);
                DiskSpaceLow?.Invoke(this, true);
            }
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}
