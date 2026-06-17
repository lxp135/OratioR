using NAudio.Wave;
using OnlyR.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace OnlyR.Core.PostProcessing;

[SupportedOSPlatform("windows7.0")]
public sealed class SilenceAnalyzer
{
    private readonly SilenceDetectionConfig _config;

    public SilenceAnalyzer(SilenceDetectionConfig? config = null)
    {
        _config = config ?? new SilenceDetectionConfig();
    }

    public List<TimeRange> Analyze(string filePath)
    {
        var silenceRanges = new List<TimeRange>();

        WaveStream? rawStream = null;
        ISampleProvider? sampleProvider = null;
        try
        {
            var afr = new AudioFileReader(filePath);
            rawStream = afr;
            sampleProvider = afr;
        }
        catch (FormatException)
        {
            var isSys = filePath.Contains("_sys.", StringComparison.OrdinalIgnoreCase);
            var sampleRate = isSys ? 48000 : 44100;
            var channels = isSys ? 2 : 1;
            var raw = new RawSourceWaveStream(
                new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                new WaveFormat(sampleRate, 16, channels));
            rawStream = raw;
            sampleProvider = raw.ToSampleProvider();
        }

        using (rawStream)
        {
            var samplesPerWindow = (int)(rawStream.WaveFormat.SampleRate * _config.WindowMs / 1000.0);
            if (samplesPerWindow < 1) samplesPerWindow = 1;
            var buffer = new float[samplesPerWindow];

            var isInSilence = false;
            TimeSpan silenceStart = TimeSpan.Zero;
            var position = TimeSpan.Zero;
            int samplesRead;

            while ((samplesRead = sampleProvider!.Read(buffer, 0, samplesPerWindow)) > 0)
            {
                var rms = CalculateRms(buffer, samplesRead);
                var rmsDb = 20.0 * Math.Log10(Math.Max(rms, 1e-10));
                var isSilent = rmsDb < _config.ThresholdDb;

                if (isSilent && !isInSilence)
                {
                    silenceStart = position;
                    isInSilence = true;
                }
                else if (!isSilent && isInSilence)
                {
                    var duration = position - silenceStart;
                    if (duration.TotalSeconds >= _config.MinSilenceDurationSeconds)
                    {
                        silenceRanges.Add(new TimeRange(silenceStart, position, "Silence"));
                    }

                    isInSilence = false;
                }

                position += TimeSpan.FromMilliseconds(_config.WindowMs * (double)samplesRead / samplesPerWindow);
            }

            if (isInSilence)
            {
                var duration = position - silenceStart;
                if (duration.TotalSeconds >= _config.MinSilenceDurationSeconds)
                {
                    silenceRanges.Add(new TimeRange(silenceStart, position, "Silence"));
                }
            }
        }

        return silenceRanges;
    }

    private static double CalculateRms(float[] buffer, int count)
    {
        double sum = 0;
        for (var i = 0; i < count; i++)
        {
            sum += buffer[i] * buffer[i];
        }

        return Math.Sqrt(sum / count);
    }
}
