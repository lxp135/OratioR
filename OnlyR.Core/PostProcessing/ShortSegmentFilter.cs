using OnlyR.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace OnlyR.Core.PostProcessing;

/// <summary>
/// Filters out audio segments shorter than the configured minimum duration.
/// </summary>
public sealed class ShortSegmentFilter
{
    private readonly double _minSeconds;

    public ShortSegmentFilter(double minSeconds = 1.0)
    {
        _minSeconds = minSeconds;
    }

    public List<TimeRange> Filter(List<TimeRange> segments)
    {
        return segments.Where(s => s.Duration.TotalSeconds >= _minSeconds).ToList();
    }
}
