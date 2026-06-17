namespace OnlyR.Core.Enums;

/// <summary>
/// Status of recording.
/// </summary>
public enum RecordingStatus
{
    Unknown,
    NotRecording,
    StopRequested,
    Recording,
    Paused,
    WaitingForDevice
}