using OnlyR.Core.Models;
using OnlyR.Services.PostProcessing;
using Serilog;
using System;
using System.Net.Http;

namespace OnlyR.Services.Upload;

/// <summary>
/// Orchestrates the upload workflow: detect ready clusters, upload, cleanup.
/// </summary>
public sealed class UploadOrchestrator : IDisposable
{
    private readonly ClusterAgeMonitor _ageMonitor;
    private readonly SpeakerApiClient _apiClient;
    private readonly RetryPolicy _retryPolicy;
    private readonly UploadQueueStore _queueStore;
    private int _consecutiveFailures;

    public UploadOrchestrator(ClusterAggregator aggregator, string speakerUrl, string apiToken)
    {
        _retryPolicy = new RetryPolicy();
        _apiClient = new SpeakerApiClient(new HttpClient(), speakerUrl, apiToken);
        _ageMonitor = new ClusterAgeMonitor(aggregator);
        _queueStore = new UploadQueueStore();
        _ageMonitor.ClusterReadyForUpload += OnClusterReady;
    }

    public event EventHandler<string>? UploadError;
    public event EventHandler<ClusterInfo>? UploadCompleted;

    public void Start()
    {
        _queueStore.Load();
        _ageMonitor.Start();
        Log.Logger.Information("UploadOrchestrator started");
    }

    public void Stop()
    {
        _ageMonitor.Stop();
    }

    public void Dispose()
    {
        _ageMonitor.Dispose();
        GC.SuppressFinalize(this);
    }

    private async void OnClusterReady(object? sender, ClusterInfo cluster)
    {
        try
        {
            if (string.IsNullOrEmpty(cluster.FilePath) || !System.IO.File.Exists(cluster.FilePath))
            {
                return;
            }

            var request = new UploadRequest
            {
                FilePath = cluster.FilePath,
                Title = $"Oratio {cluster.StartTime:yyyyMMdd_HHmm}",
                MeetingDate = cluster.StartTime
            };

            var attempts = 0;
            UploadResponse? lastResponse = null;

            while (_retryPolicy.ShouldRetry(attempts, lastResponse))
            {
                if (attempts > 0)
                {
                    await System.Threading.Tasks.Task.Delay(RetryPolicy.GetDelay(attempts));
                }

                lastResponse = await _apiClient.UploadAsync(request);
                attempts++;

                if (lastResponse.IsSuccess)
                {
                    break;
                }
            }

            if (lastResponse?.IsSuccess == true)
            {
                cluster.State = Core.Enums.ClusterState.Uploaded;
                try { System.IO.File.Delete(cluster.FilePath); } catch { }
                _queueStore.Remove(cluster.Id);
                _consecutiveFailures = 0;
                UploadCompleted?.Invoke(this, cluster);
                Log.Logger.Information("Uploaded cluster {Id}", cluster.Id);
            }
            else
            {
                _consecutiveFailures++;
                _queueStore.Add(cluster);

                var msg = lastResponse?.IsTokenInvalid == true
                    ? "API Token 无效"
                    : $"上传失败 (attempt {attempts})";

                UploadError?.Invoke(this, msg);

                if (_retryPolicy.ShouldPauseAfterMaxRetries(_consecutiveFailures))
                {
                    Log.Logger.Warning("Upload paused after {Failures} consecutive failures", _consecutiveFailures);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "UploadOrchestrator.OnClusterReady exception");
        }
    }

    public void ManualUpload(ClusterInfo cluster)
    {
        OnClusterReady(this, cluster);
    }
}
