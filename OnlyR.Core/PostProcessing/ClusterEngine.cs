using OnlyR.Core.Enums;
using OnlyR.Core.Models;
using System;
using System.Collections.Generic;

namespace OnlyR.Core.PostProcessing;

/// <summary>
/// Cluster engine: groups valid audio segments by gap threshold.
/// </summary>
public sealed class ClusterEngine
{
    private readonly TimeSpan _gapThreshold;

    public ClusterEngine(int gapMinutes = 10)
    {
        _gapThreshold = TimeSpan.FromMinutes(gapMinutes);
    }

    public List<ClusterInfo> BuildClusters(List<TimeRange> validSegments, List<TimeRange> silenceRanges, List<TimeRange> musicRanges, DateTime referenceStart)
    {
        var clusters = new List<ClusterInfo>();

        if (validSegments.Count == 0)
        {
            return clusters;
        }

        var currentCluster = new ClusterInfo
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            StartTime = referenceStart + validSegments[0].Start,
            Segments = new List<TimeRange> { validSegments[0] }
        };

        for (var i = 1; i < validSegments.Count; i++)
        {
            var gap = validSegments[i].Start - currentCluster.Segments[^1].End;

            if (gap <= _gapThreshold)
            {
                currentCluster.Segments.Add(validSegments[i]);
            }
            else
            {
                currentCluster.EndTime = referenceStart + currentCluster.Segments[^1].End;
                currentCluster.State = ClusterState.Closed;
                clusters.Add(currentCluster);

                currentCluster = new ClusterInfo
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    StartTime = referenceStart + validSegments[i].Start,
                    Segments = new List<TimeRange> { validSegments[i] }
                };
            }
        }

        currentCluster.EndTime = referenceStart + currentCluster.Segments[^1].End;
        currentCluster.State = ClusterState.Closed;
        clusters.Add(currentCluster);

        return clusters;
    }
}
