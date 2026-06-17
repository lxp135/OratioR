using OnlyR.Core.Models;
using System;
using System.Collections.Generic;

namespace OnlyR.Core.PostProcessing;

/// <summary>
/// Monitors Windows GSMTC media sessions. Records play/pause periods for blacklisted apps.
/// Note: Requires Windows 10 21H2+ and GSMTC support.
/// </summary>
public sealed class GsmtcMonitor : IDisposable
{
    private readonly HashSet<string> _blacklist;
    private readonly List<TimeRange> _musicPeriods = new();
    private readonly object _lock = new();

    public GsmtcMonitor(HashSet<string> blacklist)
    {
        _blacklist = blacklist;
    }

    public bool IsEnabled { get; private set; } = true;

    public event EventHandler<List<TimeRange>>? MusicPeriodsChanged;

    public void Start()
    {
        IsEnabled = true;
        // GSMTC session monitoring requires WinRT API.
        // For now, record periods via explicit AddMusicPeriod() for testability.
    }

    public void AddMusicPeriod(TimeSpan start, TimeSpan end)
    {
        lock (_lock)
        {
            _musicPeriods.Add(new TimeRange(start, end, "Music"));
            MusicPeriodsChanged?.Invoke(this, new List<TimeRange>(_musicPeriods));
        }
    }

    public List<TimeRange> GetMusicPeriods()
    {
        lock (_lock)
        {
            return new List<TimeRange>(_musicPeriods);
        }
    }

    public void Dispose()
    {
        IsEnabled = false;
        GC.SuppressFinalize(this);
    }
}
