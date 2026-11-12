using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DesktopFish.Interop;
using DesktopFish.Settings;

namespace DesktopFish.Tray;

/// <summary>
/// 系统托盘图标与菜单。鱼塘本身鼠标穿透，所有控制都从这里进入。
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _visibilityItem;
    private readonly ToolStripMenuItem _attractHotkeyItem;
    private readonly ToolStripMenuItem _interactHotkeyItem;
    private readonly Dictionary<double, ToolStripMenuItem> _sizeItems = new();
    private readonly Dictionary<double, ToolStripMenuItem> _opacityItems = new();
    private readonly ToolStripMenuItem _haloItem;
    private readonly ToolStripMenuItem _count4Item;
    private readonly ToolStripMenuItem _count8Item;
    private readonly ToolStripMenuItem _count14Item;
    private readonly ToolStripMenuItem _fps20Item;
    private readonly ToolStripMenuItem _fps30Item;
    private readonly ToolStripMenuItem _fps60Item;

    private Icon? _icon;
    private bool _disposed;
    private string _attractHotkeyText = "未绑定";
    private string _interactHotkeyText = "未绑定";

    public event Action? ToggleVisibilityRequested;

    public event Action<int>? FishCountSelected;

    public event Action<int>? FrameRateSelected;

    public event Action<double>? FishSizeLimitSelected;

    public event Action<double>? FishOpacitySelected;

    public event Action<bool>? HaloToggled;

    public event Action? AttractHotkeySettingsRequested;

    public event Action? InteractHotkeySettingsRequested;

    public event Action? ExitRequested;

    public TrayIconController()
    {
        _visibilityItem = new ToolStripMenuItem("显示鱼塘") { Checked = true, CheckOnClick = false };
        _visibilityItem.Click += (_, _) => ToggleVisibilityRequested?.Invoke();

        _count4Item = new ToolStripMenuItem("4 条");
        _count8Item = new ToolStripMenuItem("8 条");
        _count14Item = new ToolStripMenuItem("14 条");
        _count4Item.Click += (_, _) => SelectFishCount(4);
        _count8Item.Click += (_, _) => SelectFishCount(8);
        _count14Item.Click += (_, _) => SelectFishCount(14);

        var countMenu = new ToolStripMenuItem("鱼的数量");
        countMenu.DropDownItems.AddRange(new ToolStripItem[] { _count4Item, _count8Item, _count14Item });

        var sizeMenu = new ToolStripMenuItem("鱼的最大体型");
        foreach (double limit in AppSettings.SizeLimits)
        {
            double value = limit;
            var item = new ToolStripMenuItem(AppSettings.SizeLimitName(value));
            item.Click += (_, _) => SelectSizeLimit(value);
            _sizeItems[value] = item;
            sizeMenu.DropDownItems.Add(item);
        }

        var opacityMenu = new ToolStripMenuItem("鱼的透明度");
        foreach (double option in AppSettings.OpacityOptions)
        {
            double value = option;
            var item = new ToolStripMenuItem(AppSettings.OpacityName(value));
            item.Click += (_, _) => SelectOpacity(value);
            _opacityItems[value] = item;
            opacityMenu.DropDownItems.Add(item);
        }

        _haloItem = new ToolStripMenuItem("鱼周围荧光") { Checked = true, CheckOnClick = false };
        _haloItem.Click += (_, _) => SelectHalo(!_haloItem.Checked);

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

        var hintItem = new ToolStripMenuItem("操作提示");
        hintItem.Click += (_, _) => ShowWelcome();

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_visibilityItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(countMenu);
        menu.Items.Add(sizeMenu);
        menu.Items.Add(opacityMenu);
        menu.Items.Add(_haloItem);
        menu.Items.Add(frameRateMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_attractHotkeyItem);
        menu.Items.Add(_interactHotkeyItem);
        menu.Items.Add(hintItem);
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
        int fishCount,
        int frameRate,
        double maxFishScale,
        double opacity,
        bool showHalo,
        string attractHotkeyText,
        string interactHotkeyText)
    {
        SetFishCountChecks(fishCount);
        SetFrameRateChecks(frameRate);
        SetSizeChecks(maxFishScale);
        SetOpacityChecks(opacity);
        _haloItem.Checked = showHalo;
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
        _attractHotkeyText = text;
        _attractHotkeyItem.Text = "吸引快捷键：" + text + "…";
    }

    public void SetInteractHotkeyText(string text)
    {
        _interactHotkeyText = text;
        _interactHotkeyItem.Text = "交互快捷键：" + text + "…";
    }

    public void ShowWelcome()
    {
        string interact = _interactHotkeyText == "未绑定" ? "未绑定快捷键" : $"按住 {_interactHotkeyText}";
        string attract = _attractHotkeyText == "未绑定" ? "未绑定快捷键" : $"按住 {_attractHotkeyText}";

        ShowBalloon(
            "机械鱼塘已启用",
            $"默认鼠标穿透。{interact} 可与鱼互动；{attract} 时所有鱼会游向鼠标。右键托盘图标可切换鱼的风格、数量和快捷键。",
            ToolTipIcon.Info);
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

    private void SelectFishCount(int count)
    {
        SetFishCountChecks(count);
        FishCountSelected?.Invoke(count);
    }

    private void SelectFrameRate(int fps)
    {
        SetFrameRateChecks(fps);
        FrameRateSelected?.Invoke(fps);
    }

    private void SelectSizeLimit(double limit)
    {
        SetSizeChecks(limit);
        FishSizeLimitSelected?.Invoke(limit);
    }

    private void SelectOpacity(double opacity)
    {
        SetOpacityChecks(opacity);
        FishOpacitySelected?.Invoke(opacity);
    }

    private void SelectHalo(bool show)
    {
        _haloItem.Checked = show;
        HaloToggled?.Invoke(show);
    }

    private void SetFishCountChecks(int count)
    {
        _count4Item.Checked = count == 4;
        _count8Item.Checked = count == 8;
        _count14Item.Checked = count == 14;
    }

    private void SetFrameRateChecks(int fps)
    {
        _fps20Item.Checked = fps <= 20;
        _fps30Item.Checked = fps > 20 && fps <= 30;
        _fps60Item.Checked = fps > 30;
    }

    private void SetSizeChecks(double maxFishScale)
    {
        foreach (KeyValuePair<double, ToolStripMenuItem> pair in _sizeItems)
        {
            pair.Value.Checked = Math.Abs(pair.Key - maxFishScale) < 0.001;
        }
    }

    private void SetOpacityChecks(double opacity)
    {
        foreach (KeyValuePair<double, ToolStripMenuItem> pair in _opacityItems)
        {
            pair.Value.Checked = Math.Abs(pair.Key - opacity) < 0.001;
        }
    }

    private void ShowBalloon(string title, string text, ToolTipIcon icon)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(6000);
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
