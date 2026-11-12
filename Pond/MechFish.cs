using System;
using System.Windows;
using DesktopFishTank.Rendering;

namespace DesktopFishTank.Pond;

/// <summary>
/// 单条机械鱼的运动状态。只保存数据，绘制交给 <see cref="MechFishRenderer"/>。
/// </summary>
public sealed class MechFish
{
    public Point Position { get; set; }

    /// <summary>朝向，弧度制，0 表示朝屏幕右侧。</summary>
    public double Heading { get; set; }

    /// <summary>本帧实际速度，单位 DIP/秒。</summary>
    public double Speed { get; set; }

    /// <summary>巡游目标速度。</summary>
    public double CruiseSpeed { get; set; }

    /// <summary>鱼鳍摆动相位。</summary>
    public double Phase { get; set; }

    /// <summary>随机漫步相位，用来产生缓慢的转向变化。</summary>
    public double WanderPhase { get; set; }

    /// <summary>体型系数，0.62 - 1.25。</summary>
    public double Size { get; set; } = 1.0;

    /// <summary>远近层级，0.65 - 1.0，影响缩放与不透明度。</summary>
    public double Depth { get; set; } = 1.0;

    /// <summary>品种：荧光鱼或磷虾。决定绘制哪个矢量造型。</summary>
    public FishSpecies Species { get; set; } = FishSpecies.GlassFish;

    public FishPalette Palette { get; set; } = FishPaletteCatalog.Default;

    /// <summary>是否绘制鱼周围那层暗淡的荧光光晕。</summary>
    public bool ShowHalo { get; set; } = true;

    /// <summary>被吸引到鼠标附近时的环绕方向：+1 顺时针，-1 逆时针。</summary>
    public int OrbitDirection { get; set; } = 1;

    public bool IsDragged { get; set; }

    /// <summary>绘制缩放的硬上限，由鱼塘统一设置，用来压住个别过大的鱼。</summary>
    public double MaxRenderScale { get; set; } = double.MaxValue;

    /// <summary>绘制时的综合缩放（已按上限裁剪）。</summary>
    public double RenderScale => Math.Min(Size * Depth, MaxRenderScale);

    /// <summary>鼠标命中的近似半径（DIP）。</summary>
    public double HitRadius => 34.0 * RenderScale;
}
