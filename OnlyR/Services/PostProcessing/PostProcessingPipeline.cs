using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.PostProcessing;
using OnlyR.Services.Chunking;
using OnlyR.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace OnlyR.Services.PostProcessing;

/// <summary>
/// Orchestrates the full post-processing pipeline when a chunk is completed.
/// </summary>
public sealed class PostProcessingPipeline
{
    private readonly SilenceAnalyzer _silenceAnalyzer;
    private readonly GsmtcMonitor _gsmtcMonitor;
    private readonly MusicPeriodStore _musicStore;
    private readonly ClusterAggregator _clusterAggregator;
    private readonly StereoMerger _merger;

    public PostProcessingPipeline(GsmtcMonitor gsmtcMonitor, ClusterAggregator aggregator, string? ffmpegPath = null)
    {
        _silenceAnalyzer = new SilenceAnalyzer();
        _gsmtcMonitor = gsmtcMonitor;
        _musicStore = new MusicPeriodStore();
        _clusterAggregator = aggregator;
        _merger = new StereoMerger(ffmpegPath);
    }

    public event EventHandler<ClusterInfo>? ClusterReady;

    public void ProcessChunk(ChunkCompletedEventArgs chunkArgs)
    {
        try
        {
            Log.Logger.Information("Processing chunk: sys={Sys}, mic={Mic}", chunkArgs.SystemChunk.FilePath, chunkArgs.MicChunk.FilePath);

            var silenceRanges = _silenceAnalyzer.Analyze(chunkArgs.SystemChunk.FilePath);
            var musicRanges = _gsmtcMonitor.GetMusicPeriods();

            var chunkStartTime = chunkArgs.SystemChunk.StartTime;
            var duration = chunkArgs.SystemChunk.EndTime - chunkArgs.SystemChunk.StartTime;

            // Scale music periods to chunk time window
            var scaledMusic = new List<OnlyR.Core.Models.TimeRange>();
            foreach (var m in musicRanges)
            {
                if (m.Start.TotalSeconds <= duration.TotalSeconds && m.End.TotalSeconds <= duration.TotalSeconds)
                {
                    scaledMusic.Add(m);
                }
            }

            _clusterAggregator.ProcessChunks(silenceRanges, scaledMusic, chunkArgs, chunkStartTime);

            var closedClusters = _clusterAggregator.GetClosedClusters();

            foreach (var cluster in closedClusters)
            {
                if (string.IsNullOrEmpty(cluster.FilePath))
                {
                    var outputPath = Path.Combine(OratioPaths.GetProcessedPath(), $"cluster_{cluster.Id}.mp3");
                    var success = _merger.Merge(chunkArgs.SystemChunk.FilePath, chunkArgs.MicChunk.FilePath, outputPath);
                    if (success)
                    {
                        cluster.FilePath = outputPath;
                        ClusterReady?.Invoke(this, cluster);
                    }
                }
            }

            _musicStore.Save(_gsmtcMonitor.GetMusicPeriods());
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Post-processing pipeline failed");
        }
    }
}
