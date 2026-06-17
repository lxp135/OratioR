using Newtonsoft.Json;
using OnlyR.Model;
using Serilog;
using System;
using System.IO;

namespace OnlyR.Services.Config;

/// <summary>
/// 从安装目录读取/写入 config.json。
/// </summary>
public sealed class ConfigRepository : IConfigRepository
{
    private readonly string _configFilePath;
    private string? _originalSignature;

    public ConfigRepository()
    {
        _configFilePath = GetConfigFilePath();
    }

    public AppConfig Config { get; private set; } = AppConfig.Default;

    public void Load()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonConvert.DeserializeObject<AppConfig>(json);
                Config = config ?? AppConfig.Default;
            }
            else
            {
                Config = AppConfig.Default;
                Save();
            }

            _originalSignature = JsonConvert.SerializeObject(Config);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Could not load config.json");
            Config = AppConfig.Default;
        }
    }

    public void Save()
    {
        try
        {
            var signature = JsonConvert.SerializeObject(Config);
            if (signature == _originalSignature)
            {
                return;
            }

            var dir = Path.GetDirectoryName(_configFilePath);
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(_configFilePath, JsonConvert.SerializeObject(Config, Formatting.Indented));
            _originalSignature = signature;
            Log.Logger.Information("config.json saved");
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Could not save config.json");
        }
    }

    private static string GetConfigFilePath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, "config.json");
    }
}
