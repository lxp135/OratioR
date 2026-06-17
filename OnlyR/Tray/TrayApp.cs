using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using OnlyR.Core.PostProcessing;
using OnlyR.Model;
using OnlyR.Services.Audio;
using OnlyR.Services.Chunking;
using OnlyR.Services.Cleanup;
using OnlyR.Services.Config;
using OnlyR.Services.Options;
using OnlyR.Services.PostProcessing;
using OnlyR.Services.Upload;
using OnlyR.Utils;
using OnlyR.ViewModel.Messages;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace OnlyR.Tray;

public sealed class TrayApp : IDisposable
{
    private readonly Window _hostWindow;
    private readonly TrayIconManager _trayIcon;
    private readonly ConfigRepository _configRepo;
    private readonly AudioCaptureService _captureService;
    private readonly ChunkManager _chunkManager;
    private readonly DeviceMonitor _deviceMonitor;
    private readonly GsmtcMonitor _gsmtcMonitor;
    private PostProcessingPipeline? _pipeline;
    private ChunkCompletionHandler? _chunkHandler;
    private UploadOrchestrator? _uploadOrchestrator;
    private readonly CleanupService _cleanupService;

    public TrayApp()
    {
        _configRepo = new ConfigRepository();
        _captureService = new AudioCaptureService();
        _chunkManager = new ChunkManager(_captureService, _configRepo.Config.ChunkDurationMinutes);
        _deviceMonitor = new DeviceMonitor();
        _trayIcon = new TrayIconManager();
        _cleanupService = new CleanupService(_configRepo.Config.RetentionDays);
        _gsmtcMonitor = new GsmtcMonitor(_configRepo.Config.MusicBlacklist);

        _hostWindow = new Window
        {
            Width = 0, Height = 0,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            WindowState = WindowState.Minimized
        };

        WireBasicEvents();
    }

    public void Initialize()
    {
        _hostWindow.Show();
        _hostWindow.WindowState = WindowState.Minimized;
        _hostWindow.Visibility = Visibility.Hidden;

        _configRepo.Load();
        OratioPaths.EnsureAll();
        LogConfig.Initialize();

        if (_configRepo.Config.AutoStart)
            AutoStartHelper.EnableAutoStart();

        _cleanupService.Execute();
        _gsmtcMonitor.Start();

        InitPipelineAndUpload();
        ProcessResidualRawFiles();

        _trayIcon.SetState(TrayIconState.Initializing);
        StartRecording();
        _chunkManager.Start();
        _deviceMonitor.Start();
    }

    private void InitPipelineAndUpload()
    {
        var aggregator = new ClusterAggregator();
        _pipeline = new PostProcessingPipeline(_gsmtcMonitor, aggregator, _configRepo.Config.FfmpegPath);
        _chunkHandler = new ChunkCompletionHandler(_pipeline);

        _uploadOrchestrator = new UploadOrchestrator(aggregator, _configRepo.Config.SpeakerBaseUrl, _configRepo.Config.ApiToken);
        _uploadOrchestrator.Start();
        _uploadOrchestrator.UploadError += OnUploadError;
        _uploadOrchestrator.UploadCompleted += OnUploadCompleted;

        _captureService.ChunkCompleted += OnChunkCompleted;
        _chunkHandler.ClusterReadyForUpload += OnClusterReady;
    }

    private void StartRecording()
    {
        try
        {
            var now = DateTime.UtcNow;
            var sysCandidate = new RecordingCandidate(now, 1,
                ChunkRepository.GenerateFilePath(Core.Enums.TrackType.SystemAudio, now, 0),
                Path.Combine(OratioPaths.GetRawPath(), $"sys_final_{now:yyyyMMddHHmmss}.mp3"));
            var micCandidate = new RecordingCandidate(now, 1,
                ChunkRepository.GenerateFilePath(Core.Enums.TrackType.Microphone, now, 0),
                Path.Combine(OratioPaths.GetRawPath(), $"mic_final_{now:yyyyMMddHHmmss}.mp3"));

            var optionsService = Ioc.Default.GetService<IOptionsService>()!;
            _captureService.StartRecording(sysCandidate, micCandidate, optionsService);
            _trayIcon.SetState(TrayIconState.Recording);
            Log.Information("Recording started: sys={Sys}, mic={Mic}", sysCandidate.TempPath, micCandidate.TempPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start recording");
            _trayIcon.SetState(TrayIconState.Error);
        }
    }

    private void WireBasicEvents()
    {
        _trayIcon.MenuActionInvoked += OnMenuAction;
        _chunkManager.ChunkCreationRequested += OnChunkCreationRequested;
        _deviceMonitor.DeviceAvailabilityChanged += OnDeviceAvailabilityChanged;
        WeakReferenceMessenger.Default.Register<ShutDownApplicationMessage>(this, (_, _) => Shutdown());
        WeakReferenceMessenger.Default.Register<SessionEndingMessage>(this, (_, m) => OnSessionEnding(m));
    }

    private void ProcessResidualRawFiles()
    {
        var rawPath = OratioPaths.GetRawPath();
        if (!Directory.Exists(rawPath)) return;

        var wavFiles = Directory.GetFiles(rawPath, "*.wav").ToList();
        if (wavFiles.Count < 2) return;

        var sysFiles = wavFiles.Where(f => f.Contains("_sys.")).OrderBy(f => f).ToList();
        var micFiles = wavFiles.Where(f => f.Contains("_mic.")).OrderBy(f => f).ToList();
        var count = Math.Min(sysFiles.Count, micFiles.Count);

        for (var i = 0; i < count; i++)
        {
            var sysFile = sysFiles[i];
            var micFile = micFiles[i];
            var sysInfo = new FileInfo(sysFile);
            var micInfo = new FileInfo(micFile);

            if (sysInfo.Length < 44 || micInfo.Length < 44) continue;

            var now = DateTime.UtcNow;
            var sysChunk = new Core.Models.ChunkInfo(
                Path.GetFileNameWithoutExtension(sysFile), sysFile,
                Core.Enums.TrackType.SystemAudio, now.AddMinutes(-30), now) { FileSize = sysInfo.Length };
            var micChunk = new Core.Models.ChunkInfo(
                Path.GetFileNameWithoutExtension(micFile), micFile,
                Core.Enums.TrackType.Microphone, now.AddMinutes(-30), now) { FileSize = micInfo.Length };

            _chunkHandler?.Handle(new Core.EventArgs.ChunkCompletedEventArgs(sysChunk, micChunk));
            Log.Information("Processed residual chunk: {Sys}, {Mic}", sysFile, micFile);
        }
    }

    private void OnChunkCompleted(object? sender, Core.EventArgs.ChunkCompletedEventArgs e) => _chunkHandler?.Handle(e);

    private void OnClusterReady(object? sender, Core.Models.ClusterInfo cluster)
    {
        Log.Information("Cluster ready for upload: {Id}", cluster.Id);
        _uploadOrchestrator?.ManualUpload(cluster);
    }

    private void OnUploadError(object? sender, string error) => Log.Warning("Upload error: {Error}", error);
    private void OnUploadCompleted(object? sender, Core.Models.ClusterInfo cluster) => Log.Information("Upload completed: {Id}", cluster.Id);

    private void OnMenuAction(TrayMenuAction action)
    {
        switch (action)
        {
            case TrayMenuAction.OpenConfig:
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(configPath)) System.Diagnostics.Process.Start("notepad.exe", configPath);
                break;
            case TrayMenuAction.OpenRecordingsFolder:
                System.Diagnostics.Process.Start("explorer.exe", OratioPaths.GetRawPath());
                break;
            case TrayMenuAction.Exit:
                Shutdown();
                break;
        }
    }

    private static void TriggerManualUpload() { }
    private static void TriggerRetry() { }

    private void OnChunkCreationRequested(object? sender, EventArgs e)
    {
        try
        {
            var now = DateTime.UtcNow;
            var sysPath = ChunkRepository.GenerateFilePath(Core.Enums.TrackType.SystemAudio, now, 0);
            var micPath = ChunkRepository.GenerateFilePath(Core.Enums.TrackType.Microphone, now, 0);
            _captureService.CreateNewChunk(
                new RecordingCandidate(now, 0, sysPath, sysPath),
                new RecordingCandidate(now, 0, micPath, micPath));
        }
        catch (Exception ex) { Log.Error(ex, "Chunk creation failed"); }
    }

    private void OnDeviceAvailabilityChanged(object? sender, bool available)
    {
        if (!available) _trayIcon.SetState(TrayIconState.Error);
        else _trayIcon.SetState(TrayIconState.Recording);
    }

    private void OnSessionEnding(SessionEndingMessage message)
    {
        Log.Information("System session ending, flushing...");
        _chunkManager.FlushCurrentChunk();
        _captureService.StopRecording();
        _deviceMonitor.Stop();
        _uploadOrchestrator?.Stop();
    }

    public void Shutdown()
    {
        _chunkManager.Stop();
        _captureService.StopRecording();
        _deviceMonitor.Stop();
        _uploadOrchestrator?.Stop();
        WeakReferenceMessenger.Default.Send(new ShutDownApplicationMessage());
    }

    public void Dispose()
    {
        _chunkManager.Dispose();
        (_captureService as IDisposable)?.Dispose();
        _deviceMonitor.Dispose();
        _gsmtcMonitor.Dispose();
        _uploadOrchestrator?.Dispose();
        _pipeline = null;
        _trayIcon.Dispose();
        _hostWindow.Close();
        GC.SuppressFinalize(this);
    }
}
