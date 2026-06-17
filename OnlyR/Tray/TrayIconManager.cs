using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;

namespace OnlyR.Tray;

public enum TrayIconState
{
    Initializing,
    Recording,
    Error
}

public sealed class TrayIconManager : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;
    private TrayIconState _currentState = TrayIconState.Initializing;

    public TrayIconManager()
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "iconmic.ico");
        if (!File.Exists(iconPath))
        {
            // 尝试从资源中提取
            var asm = Assembly.GetExecutingAssembly();
            var resourceNames = asm.GetManifestResourceNames();
            iconPath = Array.Find(resourceNames, n => n.EndsWith("iconmic.ico", StringComparison.OrdinalIgnoreCase));
        }

        _taskbarIcon = new TaskbarIcon
        {
            Icon = File.Exists(iconPath) ? new System.Drawing.Icon(iconPath) : System.Drawing.SystemIcons.Application,
            ToolTipText = "Oratio"
        };

        var contextMenu = new System.Windows.Controls.ContextMenu();
        contextMenu.Items.Add(CreateMenuItem("立即上传", TrayMenuAction.UploadNow));
        contextMenu.Items.Add(CreateMenuItem("重传未完成", TrayMenuAction.RetryFailed));
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        contextMenu.Items.Add(CreateMenuItem("打开配置", TrayMenuAction.OpenConfig));
        contextMenu.Items.Add(CreateMenuItem("打开录音目录", TrayMenuAction.OpenRecordingsFolder));
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        contextMenu.Items.Add(CreateMenuItem("退出", TrayMenuAction.Exit));

        _taskbarIcon.ContextMenu = contextMenu;
    }

    public TrayIconState CurrentState => _currentState;

    public event Action<TrayMenuAction>? MenuActionInvoked;

    public void SetState(TrayIconState state)
    {
        _currentState = state;
        _taskbarIcon.ToolTipText = state switch
        {
            TrayIconState.Initializing => "Oratio - 初始化中",
            TrayIconState.Recording => "Oratio - 录音中",
            TrayIconState.Error => "Oratio - 异常",
            _ => "Oratio"
        };
    }

    public void ShowBalloon(string title, string message, BalloonIcon icon)
    {
        _taskbarIcon.ShowBalloonTip(title, message, icon);
    }

    private System.Windows.Controls.MenuItem CreateMenuItem(string header, TrayMenuAction action)
    {
        var item = new System.Windows.Controls.MenuItem { Header = header, Tag = action };
        item.Click += (_, _) => MenuActionInvoked?.Invoke(action);
        return item;
    }

    public void Dispose()
    {
        _taskbarIcon.Dispose();
        GC.SuppressFinalize(this);
    }
}
