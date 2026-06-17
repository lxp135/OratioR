using OnlyR.Core.Enums;
using System;

namespace OnlyR.Core.Recorder;

/// <summary>
/// Configuration of a recording.
/// </summary>
public class RecordingConfig
{
    public int RecordingDevice { get; set; }

    public bool UseLoopbackCapture { get; set; }

    public DateTime RecordingDate { get; set; }

    public int TrackNumber { get; set; }

    public string? DestFilePath { get; set; }

    public string? FinalFilePath { get; set; }

    public int SampleRate { get; set; }

    public int ChannelCount { get; set; }

    public int? Mp3BitRate { get; set; }

    public AudioCodec Codec { get; set; }

    public string? TrackTitle { get; set; }

    public string? AlbumName { get; set; }

    public string? Genre { get; set; }

    /// <summary>区分系统音频轨与麦克风轨。</summary>
    public TrackType TrackType { get; set; } = TrackType.SystemAudio;
}