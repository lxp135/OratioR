using CommunityToolkit.Mvvm.ComponentModel;
using OnlyR.Services.Audio;
using OnlyR.Services.Options;
using OnlyR.Utils;
using System.IO;

namespace OnlyR.ViewModel;

public class MainViewModel : ObservableObject
{
    private readonly IOptionsService _optionsService;

    public MainViewModel(
        IAudioService audioService,
        IOptionsService optionsService,
        ICommandLineService commandLineService)
    {
        _optionsService = optionsService;

        FixAnyUnfinishedRecording();
    }

    private void FixAnyUnfinishedRecording()
    {
        var tempPath = _optionsService.Options.UnfinishedRecordingTempPath;
        var finalPath = _optionsService.Options.UnfinishedRecordingFinalPath;

        if (string.IsNullOrEmpty(tempPath) ||
            string.IsNullOrEmpty(finalPath) ||
            !File.Exists(tempPath) ||
            File.Exists(finalPath))
        {
            return;
        }

        FileUtils.MoveFile(tempPath, finalPath);
    }
}