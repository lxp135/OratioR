using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.Recorder;
using OnlyR.Model;
using OnlyR.Services.Options;
using System;

namespace OnlyR.Services.Audio;

/// <summary>
/// Implements IAudioCaptureService using DualTrackRecorder.
/// </summary>
public sealed class AudioCaptureService : IAudioCaptureService, IDisposable
{
    private readonly DualTrackRecorder _recorder;

    public AudioCaptureService()
    {
        _recorder = new DualTrackRecorder();
        _recorder.ChunkCompleted += (_, args) => ChunkCompleted?.Invoke(this, args);
    }

    public bool IsRecording => _recorder.IsRecording;

    public event EventHandler<ChunkCompletedEventArgs>? ChunkCompleted;

    public void StartRecording(RecordingCandidate sysCandidate, RecordingCandidate micCandidate, IOptionsService optionsService)
    {
        var sysConfig = BuildConfig(sysCandidate, optionsService, true);
        var micConfig = BuildConfig(micCandidate, optionsService, false);

        _recorder.Start(sysConfig, micConfig);
    }

    public void StopRecording()
    {
        _recorder.Stop(false);
    }

    public void CreateNewChunk(RecordingCandidate sysCandidate, RecordingCandidate micCandidate)
    {
        var now = DateTime.UtcNow;
        var prevSysChunk = new ChunkInfo(sysCandidate.TempPath, sysCandidate.TempPath, TrackType.SystemAudio, now.AddMinutes(-1), now);
        var prevMicChunk = new ChunkInfo(micCandidate.TempPath, micCandidate.TempPath, TrackType.Microphone, now.AddMinutes(-1), now);

        _recorder.CreateNewChunk(
            new RecordingConfig
            {
                DestFilePath = sysCandidate.TempPath,
                FinalFilePath = sysCandidate.FinalPath,
                SampleRate = 44100,
                ChannelCount = 1,
                Mp3BitRate = 48,
                Codec = AudioCodec.Wav,
                RecordingDate = sysCandidate.RecordingDate,
                TrackNumber = sysCandidate.TrackNumber,
            },
            new RecordingConfig
            {
                DestFilePath = micCandidate.TempPath,
                FinalFilePath = micCandidate.FinalPath,
                SampleRate = 44100,
                ChannelCount = 1,
                Mp3BitRate = 48,
                Codec = AudioCodec.Wav,
                RecordingDate = micCandidate.RecordingDate,
                TrackNumber = micCandidate.TrackNumber,
            },
            prevSysChunk,
            prevMicChunk);
    }

    public void Dispose()
    {
        _recorder.Dispose();
        GC.SuppressFinalize(this);
    }

    private static RecordingConfig BuildConfig(RecordingCandidate candidate, IOptionsService optionsService, bool isLoopback)
    {
        return new RecordingConfig
        {
            RecordingDevice = optionsService.Options.RecordingDevice,
            UseLoopbackCapture = isLoopback,
            RecordingDate = candidate.RecordingDate,
            TrackNumber = candidate.TrackNumber,
            DestFilePath = candidate.TempPath,
            FinalFilePath = candidate.FinalPath,
            SampleRate = optionsService.Options.SampleRate,
            ChannelCount = optionsService.Options.ChannelCount,
            Mp3BitRate = optionsService.Options.Mp3BitRate,
            Codec = AudioCodec.Wav,
        };
    }
}
