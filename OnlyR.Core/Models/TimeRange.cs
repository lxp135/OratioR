using System;

namespace OnlyR.Core.Models;

/// <summary>
/// Represents a time range with an optional tag (Silence, Music, Voice).
/// </summary>
public sealed class TimeRange
{
    public TimeRange(TimeSpan start, TimeSpan end, string? tag = null)
    {
        Start = start;
        End = end;
        Tag = tag;
    }

    public TimeSpan Start { get; }
    public TimeSpan End { get; }
    public string? Tag { get; }

    public TimeSpan Duration => End - Start;

    public bool Overlaps(TimeRange other) =>
        Start < other.End && End > other.Start;

    public static TimeRange? Merge(TimeRange a, TimeRange b)
    {
        if (!a.Overlaps(b) && a.End != b.Start && b.End != a.Start)
        {
            return null;
        }

        var start = a.Start < b.Start ? a.Start : b.Start;
        var end = a.End > b.End ? a.End : b.End;
        return new TimeRange(start, end, a.Tag ?? b.Tag);
    }
}
