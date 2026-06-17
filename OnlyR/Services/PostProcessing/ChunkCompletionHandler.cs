using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.PostProcessing;

namespace OnlyR.Services.PostProcessing;

/// <summary>
/// Listens for chunk completion events and feeds them into the processing pipeline.
/// </summary>
public sealed class ChunkCompletionHandler
{
    private readonly PostProcessingPipeline _pipeline;

    public ChunkCompletionHandler(PostProcessingPipeline pipeline)
    {
        _pipeline = pipeline;
        _pipeline.ClusterReady += (_, cluster) => ClusterReadyForUpload?.Invoke(this, cluster);
    }

    public event System.EventHandler<ClusterInfo>? ClusterReadyForUpload;

    public void Handle(ChunkCompletedEventArgs args)
    {
        _pipeline.ProcessChunk(args);
    }
}
