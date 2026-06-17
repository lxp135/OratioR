using NAudio.Wave;
using OnlyR.Core.Models;
using System;
using System.Collections.Generic;

namespace OnlyR.Core.PostProcessing;

public sealed class SilenceDetectionConfig
{
    public int WindowMs { get; set; } = 100;
    public double ThresholdDb { get; set; } = -40;
    public int MinSilenceDurationSeconds { get; set; } = 2;
}
