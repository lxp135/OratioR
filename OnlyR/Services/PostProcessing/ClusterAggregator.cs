using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.PostProcessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlyR.Services.PostProcessing;

/// <summary>
/// Aggregates clusters across chunk boundaries.
/// </summary>
public sealed class ClusterAggregator
{
    private readonly List<ClusterInfo> _clusters = new();
    private readonly object _lock = new();

    public event EventHandler<ClusterInfo>? ClusterClosed;

    public void ProcessChunks(List<TimeRange> silenceRanges, List<TimeRange> musicRanges, ChunkCompletedEventArgs chunkArgs, DateTime chunkStartTime)
    {
        var totalSeconds = (chunkArgs.SystemChunk.EndTime - chunkArgs.SystemChunk.StartTime).TotalSeconds;
        var allExcluded = MergeAndSortRanges(silenceRanges, musicRanges, totalSeconds);

        var validSegments = ExtractValidSegments(allExcluded, totalSeconds);

        var filter = new ShortSegmentFilter();
        var filtered = filter.Filter(validSegments);

        var engine = new ClusterEngine();
        var newClusters = engine.BuildClusters(filtered, silenceRanges, musicRanges, chunkStartTime);

        lock (_lock)
        {
            foreach (var c in newClusters.Where(c => c.State == ClusterState.Closed))
            {
                _clusters.Add(c);
                ClusterClosed?.Invoke(this, c);
            }
        }
    }

    public List<ClusterInfo> GetClosedClusters()
    {
        lock (_lock)
        {
            return _clusters.Where(c => c.State == ClusterState.Closed).ToList();
        }
    }

    private static List<TimeRange> ExtractValidSegments(List<TimeRange> excludedRanges, double totalSeconds)
    {
        var valid = new List<TimeRange>();
        double cursor = 0;

        foreach (var excluded in excludedRanges.OrderBy(r => r.Start))
        {
            if (excluded.Start.TotalSeconds > cursor)
            {
                valid.Add(new TimeRange(TimeSpan.FromSeconds(cursor), excluded.Start));
            }

            cursor = Math.Max(cursor, excluded.End.TotalSeconds);
        }

        if (cursor < totalSeconds - 0.1)
        {
            valid.Add(new TimeRange(TimeSpan.FromSeconds(cursor), TimeSpan.FromSeconds(totalSeconds)));
        }

        return valid;
    }

    private static List<TimeRange> MergeAndSortRanges(List<TimeRange> a, List<TimeRange> b, double totalSeconds)
    {
        var all = new List<TimeRange>();
        all.AddRange(a);
        all.AddRange(b);
        all = all.OrderBy(r => r.Start).ToList();

        var merged = new List<TimeRange>();
        foreach (var range in all)
        {
            if (merged.Count > 0 && merged[^1].Overlaps(range))
            {
                var m = TimeRange.Merge(merged[^1], range);
                if (m != null)
                {
                    merged[^1] = m;
                }
            }
            else
            {
                merged.Add(range);
            }
        }

        return merged;
    }
}
