using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DesktopFishTank.Interop;

namespace DesktopFishTank.Tray;

/// <summary>
/// 系统托盘图标与菜单。鱼塘本身鼠标穿透，所有控制都从这里进入。
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _visibilityItem;
    private readonly ToolStripMenuItem _attractHotkeyItem;
    private readonly ToolStripMenuItem _interactHotkeyItem;
    private readonly ToolStripMenuItem _fps20Item;
    private readonly ToolStripMenuItem _fps30Item;
    private readonly ToolStripMenuItem _fps60Item;

    private Icon? _icon;
    private bool _disposed;

    public event Action? ToggleVisibilityRequested;

    public event Action<int>? FrameRateSelected;

    public event Action? PondSettingsRequested;

    public event Action? AttractHotkeySettingsRequested;

    public event Action? InteractHotkeySettingsRequested;

    public event Action? ExitRequested;

    public TrayIconController()
    {
        _visibilityItem = new ToolStripMenuItem("显示鱼塘") { Checked = true, CheckOnClick = false };
        _visibilityItem.Click += (_, _) => ToggleVisibilityRequested?.Invoke();

        var pondSettingsItem = new ToolStripMenuItem("鱼群设置…");
        pondSettingsItem.Click += (_, _) => PondSettingsRequested?.Invoke();

        _fps20Item = new ToolStripMenuItem("省电（20 FPS）");
        _fps30Item = new ToolStripMenuItem("标准（30 FPS）");
        _fps60Item = new ToolStripMenuItem("流畅（60 FPS）");
        _fps20Item.Click += (_, _) => SelectFrameRate(20);
        _fps30Item.Click += (_, _) => SelectFrameRate(30);
        _fps60Item.Click += (_, _) => SelectFrameRate(60);

        var frameRateMenu = new ToolStripMenuItem("帧率");
        frameRateMenu.DropDownItems.AddRange(new ToolStripItem[] { _fps20Item, _fps30Item, _fps60Item });

        _attractHotkeyItem = new ToolStripMenuItem("吸引快捷键：未设置…");
        _attractHotkeyItem.Click += (_, _) => AttractHotkeySettingsRequested?.Invoke();

        _interactHotkeyItem = new ToolStripMenuItem("交互快捷键：未设置…");
        _interactHotkeyItem.Click += (_, _) => InteractHotkeySettingsRequested?.Invoke();

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_visibilityItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(pondSettingsItem);
        menu.Items.Add(frameRateMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_attractHotkeyItem);
        menu.Items.Add(_interactHotkeyItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _icon = CreateTrayIcon();
        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "机械鱼塘",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _notifyIcon.DoubleClick += (_, _) => ToggleVisibilityRequested?.Invoke();
    }

    /// <summary>把菜单里的勾选状态与当前配置对齐。</summary>
    public void SyncState(
        int frameRate,
        string attractHotkeyText,
        string interactHotkeyText)
    {
        SetFrameRateChecks(frameRate);
        SetAttractHotkeyText(attractHotkeyText);
        SetInteractHotkeyText(interactHotkeyText);
    }

    public void SetVisibleState(bool visible)
    {
        _visibilityItem.Checked = visible;
        _visibilityItem.Text = visible ? "显示鱼塘" : "隐藏鱼塘";
    }

    public void SetAttractHotkeyText(string text)
    {
        _attractHotkeyItem.Text = "吸引快捷键：" + text + "…";
    }

    public void SetInteractHotkeyText(string text)
    {
        _interactHotkeyItem.Text = "交互快捷键：" + text + "…";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon?.Dispose();
        _icon = null;
    }

    private void SelectFrameRate(int fps)
    {
        SetFrameRateChecks(fps);
        FrameRateSelected?.Invoke(fps);
    }

    private void SetFrameRateChecks(int fps)
    {
        _fps20Item.Checked = fps <= 20;
        _fps30Item.Checked = fps > 20 && fps <= 30;
        _fps60Item.Checked = fps > 30;
    }

    private static Icon CreateTrayIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using var glow = new SolidBrush(Color.FromArgb(70, 67, 240, 255));
            graphics.FillEllipse(glow, 1, 5, 30, 22);

            using var tailBrush = new SolidBrush(Color.FromArgb(255, 38, 52, 74));
            graphics.FillPolygon(tailBrush, new[]
            {
                new PointF(9, 16), new PointF(0, 7), new PointF(2, 16), new PointF(0, 25),
            });

            using var body = new LinearGradientBrush(
                new Rectangle(5, 8, 22, 16),
                Color.FromArgb(255, 88, 116, 150),
                Color.FromArgb(255, 11, 18, 28),
                90f);
            graphics.FillEllipse(body, 5, 8, 22, 16);

            using var accentPen = new Pen(Color.FromArgb(255, 67, 240, 255), 1.4f);
            graphics.DrawEllipse(accentPen, 5, 8, 22, 16);

            using var eyeGlow = new SolidBrush(Color.FromArgb(120, 255, 79, 216));
            graphics.FillEllipse(eyeGlow, 16, 10, 11, 11);

            using var eye = new SolidBrush(Color.FromArgb(255, 255, 79, 216));
            graphics.FillEllipse(eye, 18, 12, 6, 6);

            using var rim = new Pen(Color.FromArgb(210, 221, 255, 255), 1.0f);
            graphics.DrawEllipse(rim, 18, 12, 6, 6);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }
}
