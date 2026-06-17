using Serilog;
using System;

namespace OnlyR.Services.Upload;

public sealed class RetryPolicy
{
    private const int DefaultMaxRetries = 3;

    public int MaxRetries { get; set; } = DefaultMaxRetries;

    public bool ShouldRetry(int currentAttempt, UploadResponse? lastResponse)
    {
        if (currentAttempt >= MaxRetries)
        {
            return false;
        }

        // 首次尝试（无上一次响应）始终允许
        if (lastResponse == null)
        {
            return true;
        }

        if (lastResponse.IsTokenInvalid)
        {
            return false;
        }

        return lastResponse.IsRetryable;
    }

    public static TimeSpan GetDelay(int currentAttempt)
    {
        var seconds = Math.Pow(2, currentAttempt); // 1s, 2s, 4s
        return TimeSpan.FromSeconds(seconds);
    }

    public bool ShouldPauseAfterMaxRetries(int attempts)
    {
        return attempts >= MaxRetries;
    }
}
