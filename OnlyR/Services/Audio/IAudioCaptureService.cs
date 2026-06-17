using OnlyR.Core.EventArgs;
using OnlyR.Model;
using OnlyR.Services.Options;
using System;

namespace OnlyR.Services.Audio;

/// <summary>
/// Unified audio capture interface for Oratio. Wraps DualTrackRecorder.
/// </summary>
public interface IAudioCaptureService
{
    bool IsRecording { get; }

    event System.EventHandler<ChunkCompletedEventArgs>? ChunkCompleted;

    void StartRecording(RecordingCandidate sysCandidate, RecordingCandidate micCandidate, IOptionsService optionsService);

    void StopRecording();

    void CreateNewChunk(RecordingCandidate sysCandidate, RecordingCandidate micCandidate);
}
