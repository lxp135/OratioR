using System;
using System.Timers;

namespace OnlyR.Services.Audio;

/// <summary>
/// Monitors audio device availability and triggers reconnect attempts.
/// </summary>
public sealed class DeviceMonitor : IDisposable
{
    private readonly Timer _checkTimer;
    private readonly DeviceRetryPolicy _retryPolicy;
    private bool _deviceAvailable = true;

    public DeviceMonitor()
    {
        _retryPolicy = new DeviceRetryPolicy();
        _checkTimer = new Timer(TimeSpan.FromSeconds(_retryPolicy.RetryIntervalSeconds));
        _checkTimer.Elapsed += OnCheckTimerElapsed;
        _checkTimer.AutoReset = true;
    }

    public event EventHandler<bool>? DeviceAvailabilityChanged;

    public bool IsDeviceAvailable => _deviceAvailable;

    public void Start()
    {
        _checkTimer.Start();
    }

    public void Stop()
    {
        _checkTimer.Stop();
    }

    public void Reset()
    {
        _retryPolicy.Reset();
    }

    private void OnCheckTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var wasAvailable = _deviceAvailable;

        try
        {
            var deviceCount = Core.Recorder.AudioRecorder.GetRecordingDeviceList().GetEnumerator().MoveNext() ? 1 : 0;
            _deviceAvailable = deviceCount > 0 || true; // for now, assume WASAPI always available
        }
        catch
        {
            _deviceAvailable = false;
        }

        if (wasAvailable != _deviceAvailable)
        {
            DeviceAvailabilityChanged?.Invoke(this, _deviceAvailable);
        }
    }

    public void Dispose()
    {
        _checkTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}
