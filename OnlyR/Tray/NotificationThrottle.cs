using System;
using System.Collections.Generic;

namespace OnlyR.Tray;

/// <summary>
/// Prevents repeated notifications of the same type in a short time window.
/// </summary>
public sealed class NotificationThrottle
{
    private readonly Dictionary<string, DateTime> _lastShown = new();
    private readonly TimeSpan _cooldown;

    public NotificationThrottle(TimeSpan? cooldown = null)
    {
        _cooldown = cooldown ?? TimeSpan.FromMinutes(1);
    }

    public bool ShouldShow(string notificationType)
    {
        if (_lastShown.TryGetValue(notificationType, out var lastTime))
        {
            if (DateTime.UtcNow - lastTime < _cooldown)
            {
                return false;
            }
        }

        _lastShown[notificationType] = DateTime.UtcNow;
        return true;
    }
}
