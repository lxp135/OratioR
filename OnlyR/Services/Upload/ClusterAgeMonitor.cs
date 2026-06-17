using OnlyR.Core.Enums;
using OnlyR.Core.Models;
using OnlyR.Services.PostProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace OnlyR.Services.Upload;

/// <summary>
/// Periodically checks for closed clusters that are ready for upload (last active > 10 min ago).
/// </summary>
public sealed class ClusterAgeMonitor : IDisposable
{
    private readonly ClusterAggregator _aggregator;
    private readonly Timer _timer;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _clusterGap = TimeSpan.FromMinutes(10);

    public ClusterAgeMonitor(ClusterAggregator aggregator)
    {
        _aggregator = aggregator;
        _timer = new Timer(_checkInterval);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    public event EventHandler<ClusterInfo>? ClusterReadyForUpload;

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var closedClusters = _aggregator.GetClosedClusters();
        var now = DateTime.UtcNow;

        foreach (var cluster in closedClusters.Where(c => c.State == ClusterState.Closed))
        {
            if (now - cluster.EndTime >= _clusterGap)
            {
                ClusterReadyForUpload?.Invoke(this, cluster);
            }
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}
