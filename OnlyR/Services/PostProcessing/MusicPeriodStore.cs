using Newtonsoft.Json;
using OnlyR.Core.Models;
using OnlyR.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace OnlyR.Services.PostProcessing;

public sealed class MusicPeriodStore
{
    private readonly string _filePath;

    public MusicPeriodStore()
    {
        _filePath = Path.Combine(OratioPaths.GetStatePath(), "music_periods.json");
    }

    public void Save(List<TimeRange> periods)
    {
        var data = new List<MusicPeriodDto>();
        foreach (var p in periods)
        {
            data.Add(new MusicPeriodDto { StartSeconds = p.Start.TotalSeconds, EndSeconds = p.End.TotalSeconds });
        }

        var dir = Path.GetDirectoryName(_filePath);
        if (dir != null)
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(_filePath, JsonConvert.SerializeObject(data));
    }

    public List<TimeRange> Load()
    {
        if (!File.Exists(_filePath))
        {
            return new List<TimeRange>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var data = JsonConvert.DeserializeObject<List<MusicPeriodDto>>(json) ?? new List<MusicPeriodDto>();
            var result = new List<TimeRange>();
            foreach (var d in data)
            {
                result.Add(new TimeRange(TimeSpan.FromSeconds(d.StartSeconds), TimeSpan.FromSeconds(d.EndSeconds), "Music"));
            }

            return result;
        }
        catch
        {
            return new List<TimeRange>();
        }
    }

    private sealed class MusicPeriodDto
    {
        public double StartSeconds { get; set; }
        public double EndSeconds { get; set; }
    }
}
