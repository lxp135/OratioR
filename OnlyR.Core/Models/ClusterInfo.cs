using OnlyR.Core.Enums;
using System;
using System.Collections.Generic;

namespace OnlyR.Core.Models;

public sealed class ClusterInfo
{
    public string Id { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<TimeRange> Segments { get; set; } = new();
    public ClusterState State { get; set; } = ClusterState.Active;
    public string? FilePath { get; set; }
}
