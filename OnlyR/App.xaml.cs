using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using OnlyR.Services.Audio;
using OnlyR.Services.Chunking;
using OnlyR.Services.Config;
using OnlyR.Services.Options;
using OnlyR.Tray;
using OnlyR.Utils;
using OnlyR.ViewModel;
using OnlyR.ViewModel.Messages;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

[assembly: SupportedOSPlatform("windows7.0")]

namespace OnlyR;

/// <summary>
/// Interaction logic for App.xaml.
/// </summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
[ExcludeFromCodeCoverage]
public partial class App
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    private readonly string _appString = "OnlyRAudioRecording";
    private Mutex? _appMutex;

    private TrayApp? _trayApp;

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        if (AnotherInstanceRunning())
        {
            Shutdown();
            return;
        }

        ConfigureServices();
        Current.DispatcherUnhandledException += CurrentDispatcherUnhandledException;

        _trayApp = new TrayApp();
        _trayApp.Initialize();
    }

    private void CurrentDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Log.Fatal(e.Exception, "Unhandled exception");
        Current.Shutdown();
    }

    private static void ConfigureServices()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton<IOptionsService, OptionsService>();
        serviceCollection.AddSingleton<ICommandLineService, CommandLineService>();
        serviceCollection.AddSingleton<IAudioService, AudioService>();
        serviceCollection.AddSingleton<IAudioCaptureService, AudioCaptureService>();
        serviceCollection.AddSingleton<IConfigRepository, ConfigRepository>();
        serviceCollection.AddSingleton<MainViewModel>();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        Ioc.Default.ConfigureServices(serviceProvider);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayApp?.Dispose();
        _appMutex?.Dispose();
        Log.Information("==== Exit ====");
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new SessionEndingMessage(e));
        base.OnSessionEnding(e);
    }

    private bool AnotherInstanceRunning()
    {
        _appMutex = new Mutex(true, _appString, out var newInstance);
        return !newInstance;
    }
}