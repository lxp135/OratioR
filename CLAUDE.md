# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 在此仓库中工作时提供指引。

## 关于

OnlyR 是一款基于 WPF 和 .NET 10.0 (x86) 构建的 Windows 桌面音频录音器。支持多种音频编码格式（MP3、AAC、Opus、PCM、FLAC），具备静音检测、USB 录音、音量计量和自动清理旧录音等功能。

## 构建与测试命令

```bash
# 构建
dotnet build OnlyR.slnx --configuration Release

# 运行所有测试
dotnet test --project OnlyR.Tests/OnlyR.Tests.csproj --configuration Release

# 运行单个测试类
dotnet test --project OnlyR.Tests/OnlyR.Tests.csproj --configuration Release --filter "ClassName=TestCommandLineParser"

# 发布（通过 Inno Setup 生成安装包和便携版 zip）
CreateDeliverables.cmd
```

测试使用 **TUnit** 框架（而非 xUnit/NUnit），搭配 **Rocks** 进行 Mock 模拟。

## 架构

解决方案包含三个项目：

- **OnlyR.Core** — 平台无关的业务逻辑。包含音频录音引擎（`Recorder/AudioRecorder.cs`），封装了 NAudio/WASAPI。无 UI 依赖。
- **OnlyR** — WPF 应用程序（MVVM）。通过注入的服务消费 OnlyR.Core。
- **OnlyR.Tests** — 两个项目的单元测试。

### 关键架构模式

**基于 CommunityToolkit.Mvvm 的 MVVM**：ViewModel 位于 `OnlyR/ViewModel/` 目录。页面包括 `RecordingPage` 和 `SettingsPage`，由 `MainViewModel` 协调。页面导航使用 `WeakReferenceMessenger`，配合 `ViewModel/Messages/` 中的类型化消息类。

**依赖注入**：`App.xaml.cs` 引导 `Microsoft.Extensions.DependencyInjection` 并暴露 `Ioc.Default`。所有服务均注册为单例。

**服务层** (`OnlyR/Services/`)：八个服务类别分别处理不同的关注点——`AudioService` 封装 `AudioRecorder`，`OptionsService` 将设置持久化到 Windows 注册表，`RecordingDestinationService` 管理按日期组织的文件夹层级，`SilenceService` 实现静音自动停止，`CopyRecordingsService`/`DriveEjectionService` 处理 USB 工作流，`PurgeRecordingsService` 清理旧录音。

**包版本管理**：所有 NuGet 版本集中在 `Directory.Packages.props` 中。全局构建设置（可空性、分析器、警告即错误）在 `Directory.Build.props` 中。

### 音频管线

`AudioRecorder`（OnlyR.Core）使用 NAudio WASAPI 捕获 → `SampleAggregator`（音量事件）→ 编码器特定编码（MP3 使用 NAudio.Lame 等）。`VolumeFader` 处理录音开始/结束时的淡入/淡出。

## 重要约束

- **不要建议修改本地化资源文件**（`.resx` 文件）。翻译工作通过独立的本地化流程在外部管理。
- 目标平台仅为 **Windows x86**。不要引入跨平台抽象。
- 全局启用可空引用类型——所有新代码必须为空安全。
- 代码分析使用内置的 .NET 分析器（`AnalysisLevel=latest-recommended`）以及 `Roslynator.Analyzers`，全部通过 `.editorconfig` 配置。
- 警告视为错误：`Directory.Build.props` 中设置了 `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`。

## 开发工具链

- 格式/风格由 [lefthook](https://github.com/evilmartians/lefthook) pre-commit 钩子强制执行，该钩子对暂存文件运行 `dotnet format`。开发工具（lefthook）由 [mise](https://mise.jdx.dev/) 管理——在全新检出后运行 `mise install && mise run setup` 以安装固定版本的工具并配置 git 钩子。
