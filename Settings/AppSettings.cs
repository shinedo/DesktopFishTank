using System;
using DesktopFish.Input;

namespace DesktopFish.Settings;

/// <summary>持久化到 LocalAppData 的用户配置。</summary>
public sealed class AppSettings
{
    public int FishCount { get; set; } = 8;

    /// <summary>鱼的最大绘制缩放，3.0 表示不设实际限制。</summary>
    public double MaxFishScale { get; set; } = 3.0;

    /// <summary>体型上限的可选档位，托盘菜单与设置共用。</summary>
    public static readonly double[] SizeLimits = { 0.8, 1.1, 1.4, 3.0 };

    public static string SizeLimitName(double limit)
    {
        return limit switch
        {
            <= 0.8 => "小巧（≤0.8×）",
            <= 1.1 => "标准（≤1.1×）",
            <= 1.4 => "较大（≤1.4×）",
            _ => "不限制",
        };
    }

    /// <summary>鱼的整体不透明度，1.0 为完全不透明。</summary>
    public double FishOpacity { get; set; } = 1.0;

    /// <summary>透明度可选档位，托盘菜单共用。</summary>
    public static readonly double[] OpacityOptions = { 0.4, 0.6, 0.8, 1.0 };

    public static string OpacityName(double opacity)
    {
        return $"{(int)Math.Round(opacity * 100)}%";
    }

    /// <summary>是否绘制鱼周围那层暗淡的荧光光晕。</summary>
    public bool ShowHalo { get; set; } = true;

    public int FrameRate { get; set; } = 30;

    public HotkeyBinding? AttractHotkey { get; set; } = HotkeyBinding.DefaultAttract;

    public HotkeyBinding? InteractHotkey { get; set; } = HotkeyBinding.DefaultInteract;

    public double ResolveMaxFishScale()
    {
        return Math.Clamp(MaxFishScale, 0.4, 3.0);
    }

    public double ResolveFishOpacity()
    {
        return Math.Clamp(FishOpacity, 0.15, 1.0);
    }

    public HotkeyBinding ResolveAttractHotkey()
    {
        return AttractHotkey ?? HotkeyBinding.DefaultAttract;
    }

    public HotkeyBinding ResolveInteractHotkey()
    {
        return InteractHotkey ?? HotkeyBinding.DefaultInteract;
    }
}
