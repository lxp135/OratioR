using Microsoft.Win32;
using System;
using System.Reflection;

namespace OnlyR.Utils;

public static class AutoStartHelper
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue("Oratio") != null;
    }

    public static void EnableAutoStart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        var exePath = Assembly.GetEntryAssembly()?.Location ?? Environment.ProcessPath;
        if (exePath != null)
        {
            key?.SetValue("Oratio", $"\"{exePath}\"");
        }
    }

    public static void DisableAutoStart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        try
        {
            key?.DeleteValue("Oratio", throwOnMissingValue: false);
        }
        catch
        {
            // permission denied, ignore
        }
    }
}
