using OnlyR.Services.Audio;
using System;

namespace OnlyR.Services.Audio;

/// <summary>
/// Retry policy for audio device reconnection. 60-second intervals, max retries before giving up.
/// </summary>
public sealed class DeviceRetryPolicy
{
    private const int DefaultRetryIntervalSeconds = 60;
    private const int DefaultMaxRetries = 10;
    private int _retryCount;

    public int RetryIntervalSeconds { get; set; } = DefaultRetryIntervalSeconds;
    public int MaxRetries { get; set; } = DefaultMaxRetries;

    public bool ShouldRetry()
    {
        return _retryCount < MaxRetries;
    }

    public void RecordAttempt()
    {
        _retryCount++;
    }

    public void Reset()
    {
        _retryCount = 0;
    }
}
