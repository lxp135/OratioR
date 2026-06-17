using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.Recorder;
using System;

namespace OnlyR.Core.Recorder;

/// <summary>
/// Coordinates two independent AudioRecorder instances: one for system audio (WASAPI loopback)
/// and one for microphone (WaveIn). Handles synchronized start/stop and chunk completion.
/// </summary>
public sealed class DualTrackRecorder : IDisposable
{
    private readonly AudioRecorder _systemRecorder;
    private readonly AudioRecorder _micRecorder;
    private bool _isRecording;

    public DualTrackRecorder()
    {
        _systemRecorder = new AudioRecorder();
        _micRecorder = new AudioRecorder();
    }

    public bool IsRecording => _isRecording;

    public event EventHandler<ChunkCompletedEventArgs>? ChunkCompleted;

    public void Start(RecordingConfig sysConfig, RecordingConfig micConfig)
    {
        if (_isRecording)
        {
            return;
        }

        sysConfig.TrackType = TrackType.SystemAudio;
        sysConfig.UseLoopbackCapture = true;

        micConfig.TrackType = TrackType.Microphone;
        micConfig.UseLoopbackCapture = false;

        _systemRecorder.Start(sysConfig);
        _micRecorder.Start(micConfig);

        _isRecording = true;
    }

    public void Stop(bool fadeOut)
    {
        if (!_isRecording)
        {
            return;
        }

        _systemRecorder.Stop(fadeOut);
        _micRecorder.Stop(fadeOut);

        _isRecording = false;
    }

    public void CreateNewChunk(RecordingConfig sysConfig, RecordingConfig micConfig, ChunkInfo prevSysChunk, ChunkInfo prevMicChunk)
    {
        ChunkCompleted?.Invoke(this, new ChunkCompletedEventArgs(prevSysChunk, prevMicChunk));

        sysConfig.TrackType = TrackType.SystemAudio;
        sysConfig.UseLoopbackCapture = true;

        micConfig.TrackType = TrackType.Microphone;
        micConfig.UseLoopbackCapture = false;

        _systemRecorder.Stop(false);
        _micRecorder.Stop(false);

        _systemRecorder.Start(sysConfig);
        _micRecorder.Start(micConfig);
    }

    public void Dispose()
    {
        _systemRecorder.Dispose();
        _micRecorder.Dispose();
        GC.SuppressFinalize(this);
    }
}
