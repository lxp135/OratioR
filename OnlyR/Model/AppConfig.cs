using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OnlyR.Model;

/// <summary>
/// Oratio 应用程序配置，从安装目录 config.json 反序列化。
/// </summary>
public sealed class AppConfig
{
    // ===== Speaker =====
    public string SpeakerBaseUrl { get; set; } = "https://speaker.example.com";
    public string ApiToken { get; set; } = string.Empty;

    // ===== 录制参数 =====
    public int BitrateKbps { get; set; } = 48;
    public int SampleRate { get; set; } = 44100;
    public int ChannelCount { get; set; } = 1;

    // ===== 切片与聚类 =====
    public int ChunkDurationMinutes { get; set; } = 30;
    public int ClusterGapMinutes { get; set; } = 10;
    public int MinSegmentSeconds { get; set; } = 1;
    public int? MaxClusterHours { get; set; } = 6;

    // ===== 静音检测 =====
    public int SilenceThresholdDb { get; set; } = -40;
    public int SilenceWindowMs { get; set; } = 100;
    public int MinSilenceDurationSeconds { get; set; } = 2;

    // ===== 数据管理 =====
    public int RetentionDays { get; set; } = 7;

    // ===== GSMTC 音乐过滤 =====
    public bool GsmtcEnabled { get; set; } = true;
    public HashSet<string> MusicBlacklist { get; set; } = new()
    {
        "NetEase.CloudMusic", "Tencent.QQMusic", "Spotify",
        "kwmusic", "kugou", "foobar2000"
    };

    // ===== ffmpeg =====
    public string? FfmpegPath { get; set; }

    // ===== 启动 =====
    public bool AutoStart { get; set; } = true;
    public bool StartMinimized { get; set; } = true;

    [JsonIgnore]
    public static AppConfig Default => new();
}
