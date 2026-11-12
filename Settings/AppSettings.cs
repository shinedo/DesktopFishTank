using System;
using System.Collections.Generic;
using DesktopFishTank.Input;

namespace DesktopFishTank.Settings;

/// <summary>持久化到 LocalAppData 的用户配置。</summary>
public sealed class AppSettings
{
    public int FishCount { get; set; } = 8;

    /// <summary>鱼的最大绘制缩放，3.0 表示不设实际限制。</summary>
    public double MaxFishScale { get; set; } = 3.0;

    /// <summary>鱼的整体不透明度，1.0 为完全不透明。</summary>
    public double FishOpacity { get; set; } = 1.0;

    /// <summary>是否绘制鱼周围那层暗淡的荧光光晕。</summary>
    public bool ShowHalo { get; set; } = true;

    /// <summary>允许出现的配色名；null 或空表示全部启用。</summary>
    public List<string>? EnabledPalettes { get; set; }

    /// <summary>允许出现的品种名（荧光鱼、磷虾）；null 或空表示全部启用。</summary>
    public List<string>? EnabledSpecies { get; set; }

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
