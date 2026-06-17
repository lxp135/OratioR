# Oratio 言达 — Windows 桌面音频采集器

基于 [OnlyR](https://github.com/AntonyCorbett/OnlyR) 改造的零操作 Windows 托盘应用。全时段后台录制系统音频和麦克风，自动剔除静音、过滤音乐客户端播放时段，批量后处理后上传至 Speaker 做 STT 转录和 AI 分析。

## 核心功能

- **双轨独立录制** — WASAPI Loopback（系统音频）+ WaveIn（麦克风），同步采集
- **分钟切片** — 每 30 分钟自动切分 raw 文件，平衡处理粒度与延迟
- **智能裁剪** — 离线 RMS 静音分析 + Windows GSMTC 音乐客户端识别，10 分钟规则聚类有效对话
- **自动上传** — cluster 关闭后自动通过 HTTP multipart 上传到 Speaker，支持失败重试 3 次
- **后台托盘** — 主窗口隐藏，常驻系统托盘，开机自启，状态图标区分录音/错误
- **数据管理** — 录音存储在 `%LOCALAPPDATA%\Oratio\`，7 天自动清理
- **设备跟随** — 播放/录音设备切换时自动重新初始化，蓝牙断开静默等待重连
- **睡眠保护** — 系统睡眠前刷写当前 chunk，唤醒后自动恢复录制

## 系统要求

- Windows 10 21H2+ / Windows 11
- .NET 10.0 Desktop Runtime (x86)
- 2GB 内存
- 100MB 磁盘空间 + 录音存储（约 1.5GB/天，7 天循环）
- ffmpeg（需在 PATH 中或放置于安装目录）
- 音频播放设备和录音设备（可选，无设备时不会崩溃）

## 快速开始

### 安装与配置

```bash
# 1. 构建项目
dotnet build OnlyR.slnx --configuration Release

# 2. 编辑 config.json（在构建输出目录 OnlyR\bin\Release\net10.0-windows\）
#    填入 Speaker 服务器地址和 API Token
```

`config.json` 示例：

```json
{
  "SpeakerBaseUrl": "http://your-speaker-server:8899",
  "ApiToken": "your-api-token",
  "BitrateKbps": 48,
  "SampleRate": 44100,
  "ChannelCount": 1,
  "ChunkDurationMinutes": 30,
  "ClusterGapMinutes": 10,
  "MinSegmentSeconds": 1,
  "MaxClusterHours": 6,
  "SilenceThresholdDb": -40,
  "SilenceWindowMs": 100,
  "MinSilenceDurationSeconds": 2,
  "RetentionDays": 7,
  "GsmtcEnabled": true,
  "MusicBlacklist": ["NetEase.CloudMusic", "Tencent.QQMusic", "Spotify"],
  "FfmpegPath": null,
  "AutoStart": true,
  "StartMinimized": true
}
```

### 运行

```bash
# 启动托盘应用
start OnlyR\bin\Release\net10.0-windows\OnlyR.exe
```

启动后系统托盘出现图标，自动开始双轨录制。右键托盘图标可打开菜单。

## 构建

```bash
# 构建
dotnet build OnlyR.slnx --configuration Release

# 运行测试
dotnet test --project OnlyR.Tests/OnlyR.Tests.csproj --configuration Release

# 发布（通过 Inno Setup 生成安装包）
CreateDeliverables.cmd
```

## 架构

```
OnlyR.slnx
├── OnlyR.Core/              平台无关业务逻辑
│   ├── Recorder/             DualTrackRecorder + AudioRecorder (NAudio)
│   ├── PostProcessing/       SilenceAnalyzer, GsmtcMonitor, ClusterEngine
│   ├── Enums/                TrackType, RecordingStatus, ClusterState
│   ├── EventArgs/            录音状态变更、切片完成事件
│   └── Models/               ChunkInfo, ClusterInfo, TimeRange
│
├── OnlyR/                    WPF 托盘应用
│   ├── Tray/                 TrayIconManager, TrayApp（托盘入口）
│   ├── Services/
│   │   ├── Audio/            AudioCaptureService, DeviceMonitor
│   │   ├── Chunking/         ChunkManager（30min切片）, ChunkRepository
│   │   ├── Config/           config.json 读写
│   │   ├── PostProcessing/   Pipeline, ClusterAggregator, StereoMerger
│   │   ├── Upload/           SpeakerApiClient, UploadOrchestrator, RetryPolicy
│   │   ├── Cleanup/          CleanupService（7天）, DiskSpaceMonitor
│   │   └── Recovery/         SessionRecoveryService（跨会话恢复）
│   ├── Model/                AppConfig
│   └── Utils/                FileUtils, OratioPaths, FfmpegHelper, AutoStartHelper
│
└── OnlyR.Tests/              TUnit + Rocks 单元测试
```

### 数据流

```
系统音频 + 麦克风
     │
     ▼
DualTrackRecorder (NAudio)
     │ 每30分钟切分
     ▼
raw/*.wav (双轨原始文件)
     │
     ▼ 后处理管线
SilenceAnalyzer (RMS静音) + GsmtcMonitor (音乐识别)
     │
     ▼
ClusterEngine (10分钟规则聚类)
     │
     ▼
ffmpeg (双声道合并为 MP3)
     │
     ▼
processed/cluster_*.mp3
     │
     ▼ 自动上传
Speaker API (HTTP multipart)
     │ 202 Accepted
     ▼
Speaker → STT转录 → AI分析
```

### 目录结构

```
%LOCALAPPDATA%\Oratio\
├── recordings/
│   ├── raw/           30分钟 raw 切片（保留 7 天）
│   ├── processed/     合并后待上传文件（上传成功即删）
│   └── state/         状态文件（music_periods.json, upload_queue.json）
└── logs/              日志文件（保留 28 天）

安装目录\
├── OnlyR.exe
├── config.json        配置文件（手动编辑）
└── ffmpeg.exe         音频处理（可选，也可放入 PATH）
```

## 技术栈

| 类别 | 技术 |
|------|------|
| 运行时 | .NET 10.0 (x86) |
| UI | WPF + Hardcodet.NotifyIcon.Wpf |
| MVVM/DI | CommunityToolkit.Mvvm + Microsoft.Extensions.DependencyInjection |
| 音频 | NAudio 2.3.0 (Wasapi + Lame + WinForms) |
| 序列化 | Newtonsoft.Json |
| 日志 | Serilog |
| 测试 | TUnit + Rocks |
| 音频处理 | ffmpeg |

## 许可证

本项目基于 [OnlyR](https://github.com/AntonyCorbett/OnlyR)（MIT License），Oratio 改造部分同样遵循 [MIT 许可证](LICENSE)。
