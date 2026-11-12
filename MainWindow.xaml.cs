using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopFishTank.Input;
using DesktopFishTank.Interop;
using DesktopFishTank.Pond;
using DesktopFishTank.Rendering;
using DesktopFishTank.Settings;
using DesktopFishTank.Tray;

namespace DesktopFishTank;

/// <summary>
/// 全屏透明置顶层。默认鼠标穿透，按住用户绑定的交互快捷键（默认 Ctrl + Alt）时临时可交互，
/// 按住吸引快捷键时所有鱼游向鼠标。
/// </summary>
public partial class MainWindow : Window
{
    private const int DefaultFrameRate = 30;

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _hotkeyTimer;
    private readonly DispatcherTimer _topmostTimer;
    private readonly AppSettings _settings = SettingsStore.Load();

    private TrayIconController? _tray;
    private IntPtr _hwnd = IntPtr.Zero;
    private double _lastFrameTime;
    private double _frameInterval = 1.0 / DefaultFrameRate;
    private bool _initialized;
    private bool _isInteractive;
    private bool _isPondVisible = true;
    private bool _exitRequested;
    private Point? _mousePosition;
    private MechFish? _dragFish;

    public MainWindow()
    {
        InitializeComponent();

        _hotkeyTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(70),
        };
        _hotkeyTimer.Tick += OnHotkeyTick;

        _topmostTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(4),
        };
        _topmostTimer.Tick += OnTopmostTick;

        Loaded += OnLoaded;
        SizeChanged += OnWindowSizeChanged;
        Closed += OnWindowClosed;

        CompositionTarget.Rendering += OnRendering;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        ApplyBaseWindowStyles();
        FitToVirtualScreen();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Alt+F4 或系统关闭只隐藏到托盘，真正退出走托盘菜单。
        if (!_exitRequested)
        {
            e.Cancel = true;
            SetPondVisible(false);
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isInteractive)
        {
            return;
        }

        Point position = e.GetPosition(Pond);
        _mousePosition = position;

        if (_dragFish is { IsDragged: true })
        {
            Point previous = _dragFish.Position;
            _dragFish.Position = position;

            double dx = position.X - previous.X;
            double dy = position.Y - previous.Y;
            if (Math.Abs(dx) + Math.Abs(dy) > 0.6)
            {
                _dragFish.Heading = Math.Atan2(dy, dx);
            }
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (!_isInteractive || e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        Point position = e.GetPosition(Pond);
        _dragFish = Pond.Engine.FindFishAt(position);
        if (_dragFish != null)
        {
            _dragFish.IsDragged = true;
            _dragFish.Position = position;
            Pond.Engine.DraggedFish = _dragFish;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.ChangedButton == MouseButton.Left)
        {
            ReleaseDrag();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_initialized)
        {
            _initialized = true;

            _hwnd = new WindowInteropHelper(this).Handle;
            ApplyBaseWindowStyles();
            UpdatePondBounds();

            Pond.Engine.SetMaxRenderScale(_settings.ResolveMaxFishScale());
            Pond.Engine.SetFishOpacity(_settings.ResolveFishOpacity());
            Pond.Engine.SetShowHalo(_settings.ShowHalo);
            Pond.Engine.SetEnabledPalettes(ResolveEnabledPaletteIndices(_settings.EnabledPalettes));
            Pond.Engine.SetFishCount(_settings.FishCount);
            _frameInterval = 1.0 / Math.Clamp(_settings.FrameRate, 5, 120);

            CreateTray();
            _hotkeyTimer.Start();
            _topmostTimer.Start();
        }

        FitToVirtualScreen();
        _lastFrameTime = _clock.Elapsed.TotalSeconds;
        UpdatePondBounds();
    }

    private void CreateTray()
    {
        if (_tray != null)
        {
            return;
        }

        _tray = new TrayIconController();
        _tray.SyncState(
            _settings.FrameRate,
            _settings.ResolveAttractHotkey().DisplayName,
            _settings.ResolveInteractHotkey().DisplayName);

        _tray.ToggleVisibilityRequested += TogglePondVisibility;
        _tray.FrameRateSelected += OnFrameRateSelected;
        _tray.PondSettingsRequested += OpenPondSettings;
        _tray.AttractHotkeySettingsRequested += OpenAttractHotkeySettings;
        _tray.InteractHotkeySettingsRequested += OpenInteractHotkeySettings;
        _tray.ExitRequested += ExitApplication;
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePondBounds();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _hotkeyTimer.Stop();
        _topmostTimer.Stop();
        CompositionTarget.Rendering -= OnRendering;

        _tray?.Dispose();
        _tray = null;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isPondVisible || _hwnd == IntPtr.Zero)
        {
            return;
        }

        double now = _clock.Elapsed.TotalSeconds;
        double elapsed = now - _lastFrameTime;
        if (elapsed < _frameInterval)
        {
            return;
        }

        _lastFrameTime = now;

        bool attracting = _settings.ResolveAttractHotkey().Matches();
        Point? attractTarget = null;
        if (attracting && TryGetCursorInPond(out Point cursor))
        {
            attractTarget = cursor;
        }

        Pond.Engine.MouseReacting = _isInteractive;
        Pond.Engine.MousePosition = _isInteractive ? _mousePosition : null;
        Pond.Engine.AttractMode = attracting;
        Pond.Engine.AttractTarget = attractTarget;
        Pond.Engine.Update(elapsed);
        Pond.InvalidateVisual();
    }

    private void OnHotkeyTick(object? sender, EventArgs e)
    {
        // 吸引键若与交互键重叠，优先保证鱼塘保持穿透。
        bool attracting = _settings.ResolveAttractHotkey().Matches();
        bool wantInteractive = _settings.ResolveInteractHotkey().Matches() && !attracting;

        if (wantInteractive != _isInteractive)
        {
            SetInteractive(wantInteractive);
        }
    }

    private void OnTopmostTick(object? sender, EventArgs e)
    {
        if (_hwnd == IntPtr.Zero || !_isPondVisible)
        {
            return;
        }

        // 有些全屏程序会把置顶窗口压下去，定期重申 TOPMOST。
        NativeMethods.SetWindowPos(
            _hwnd,
            NativeMethods.HWND_TOPMOST,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    private void SetInteractive(bool interactive)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        _isInteractive = interactive;

        int style = NativeMethods.GetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE);
        if (interactive)
        {
            style &= ~NativeMethods.WS_EX_TRANSPARENT;
        }
        else
        {
            style |= NativeMethods.WS_EX_TRANSPARENT;
        }

        NativeMethods.SetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE, style);

        if (!interactive)
        {
            ReleaseDrag();
            _mousePosition = null;
        }
    }

    private void ApplyBaseWindowStyles()
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        int style = NativeMethods.GetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE);
        style |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;

        if (!_isInteractive)
        {
            style |= NativeMethods.WS_EX_TRANSPARENT;
        }

        NativeMethods.SetWindowLong(_hwnd, NativeMethods.GWL_EXSTYLE, style);
    }

    private void FitToVirtualScreen()
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        int x = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        int y = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

        if (width <= 0 || height <= 0)
        {
            width = (int)SystemParameters.PrimaryScreenWidth;
            height = (int)SystemParameters.PrimaryScreenHeight;
        }

        NativeMethods.SetWindowPos(
            _hwnd,
            NativeMethods.HWND_TOPMOST,
            x,
            y,
            width,
            height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    private void UpdatePondBounds()
    {
        double width = Pond.ActualWidth > 1 ? Pond.ActualWidth : ActualWidth;
        double height = Pond.ActualHeight > 1 ? Pond.ActualHeight : ActualHeight;

        if (width > 1 && height > 1)
        {
            Pond.Engine.SetBounds(new Rect(0, 0, width, height));
        }
    }

    /// <summary>把全局光标位置换算成鱼塘窗口内的 DIP 坐标。</summary>
    private bool TryGetCursorInPond(out Point point)
    {
        point = default;

        if (!NativeMethods.GetCursorPos(out NativeMethods.POINT cursor))
        {
            return false;
        }

        int virtualLeft = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        int virtualTop = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        DpiScale dpi = VisualTreeHelper.GetDpi(this);

        point = new Point(
            (cursor.X - virtualLeft) / dpi.DpiScaleX,
            (cursor.Y - virtualTop) / dpi.DpiScaleY);
        return true;
    }

    private void TogglePondVisibility()
    {
        SetPondVisible(!_isPondVisible);
    }

    private void SetPondVisible(bool visible)
    {
        _isPondVisible = visible;

        if (visible)
        {
            Show();
            FitToVirtualScreen();
            _lastFrameTime = _clock.Elapsed.TotalSeconds;
            UpdatePondBounds();
        }
        else
        {
            SetInteractive(false);
            Hide();
        }

        _tray?.SetVisibleState(visible);
    }

    private void OnFrameRateSelected(int fps)
    {
        _frameInterval = 1.0 / Math.Clamp(fps, 5, 120);
        _settings.FrameRate = fps;
        SettingsStore.Save(_settings);
    }

    /// <summary>打开鱼群设置。滑块拖动时即时生效，关闭窗口时统一落盘。</summary>
    private void OpenPondSettings()
    {
        // 设置窗口和鱼塘都是 Topmost，暂停主窗口的置顶重申，避免设置窗口被压到后面。
        _topmostTimer.Stop();

        try
        {
            var window = new PondSettingsWindow(
                _settings.FishCount,
                _settings.ResolveMaxFishScale(),
                _settings.ResolveFishOpacity(),
                _settings.ShowHalo,
                _settings.EnabledPalettes)
            {
                Topmost = true,
            };

            window.Changed += OnPondSettingsChanged;
            window.ShowDialog();
            SettingsStore.Save(_settings);
        }
        finally
        {
            if (_isPondVisible)
            {
                _topmostTimer.Start();
            }
        }
    }

    private void OnPondSettingsChanged(PondSettingsValues values)
    {
        Pond.Engine.SetFishCount(values.FishCount);
        Pond.Engine.SetMaxRenderScale(values.MaxRenderScale);
        Pond.Engine.SetFishOpacity(values.Opacity);
        Pond.Engine.SetShowHalo(values.ShowHalo);
        Pond.Engine.SetEnabledPalettes(ResolveEnabledPaletteIndices(values.EnabledPalettes));

        _settings.FishCount = values.FishCount;
        _settings.MaxFishScale = values.MaxRenderScale;
        _settings.FishOpacity = values.Opacity;
        _settings.ShowHalo = values.ShowHalo;
        _settings.EnabledPalettes = new List<string>(values.EnabledPalettes);

        Pond.InvalidateVisual();
    }

    /// <summary>把配置里的配色名换算成下标；名字失效或为空时回退到全部启用。</summary>
    private static List<int> ResolveEnabledPaletteIndices(IReadOnlyList<string>? names)
    {
        if (names == null || names.Count == 0)
        {
            return FishPaletteCatalog.AllIndices();
        }

        var indices = new List<int>();
        foreach (string name in names)
        {
            int index = FishPaletteCatalog.IndexOfName(name);
            if (index >= 0)
            {
                indices.Add(index);
            }
        }

        return indices.Count > 0 ? indices : FishPaletteCatalog.AllIndices();
    }

    private void OpenAttractHotkeySettings()
    {
        EditHotkey(
            "鱼群吸引快捷键",
            "按住该组合键时，所有鱼会转向鼠标位置并加速游过去。",
            "按住该组合键，所有鱼会游向鼠标",
            "未绑定快捷键，鱼群不会响应鼠标",
            _settings.ResolveAttractHotkey(),
            _settings.ResolveInteractHotkey(),
            binding =>
            {
                _settings.AttractHotkey = binding;
                _tray?.SetAttractHotkeyText(binding.DisplayName);
            });
    }

    private void OpenInteractHotkeySettings()
    {
        EditHotkey(
            "鼠标交互快捷键",
            "按住该组合键时鱼塘临时可交互：鼠标靠近会把鱼惊走，左键可拖拽鱼。默认按住 Ctrl + Alt。",
            "按住该组合键，鱼塘临时可交互",
            "未绑定快捷键，鱼塘将保持完全穿透，无法拖拽鱼",
            _settings.ResolveInteractHotkey(),
            _settings.ResolveAttractHotkey(),
            binding =>
            {
                _settings.InteractHotkey = binding;
                _tray?.SetInteractHotkeyText(binding.DisplayName);
                SetInteractive(false);
            });
    }

    private void EditHotkey(
        string title,
        string description,
        string hintWhenBound,
        string hintWhenEmpty,
        HotkeyBinding current,
        HotkeyBinding conflict,
        Action<HotkeyBinding> apply)
    {
        // 设置窗口和鱼塘都是 Topmost，暂停主窗口的置顶重申，避免设置窗口被压到后面。
        _topmostTimer.Stop();

        try
        {
            var window = new SettingsWindow(
                title,
                description,
                hintWhenBound,
                hintWhenEmpty,
                current,
                conflict)
            {
                Topmost = true,
            };

            bool? accepted = window.ShowDialog();
            if (accepted == true)
            {
                apply(window.Result);
                SettingsStore.Save(_settings);
            }
        }
        finally
        {
            if (_isPondVisible)
            {
                _topmostTimer.Start();
            }
        }
    }

    private void ReleaseDrag()
    {
        if (_dragFish == null)
        {
            return;
        }

        // 松手时给一个短促加速，模拟鱼受惊窜开。
        _dragFish.IsDragged = false;
        _dragFish.Speed = Math.Max(_dragFish.Speed, _dragFish.CruiseSpeed * 2.4);
        _dragFish = null;
        Pond.Engine.DraggedFish = null;
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        _hotkeyTimer.Stop();
        _topmostTimer.Stop();
        CompositionTarget.Rendering -= OnRendering;

        _tray?.Dispose();
        _tray = null;

        Application.Current.Shutdown();
    }
}
