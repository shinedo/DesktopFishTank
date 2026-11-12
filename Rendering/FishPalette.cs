using System;
using System.Windows;
using System.Windows.Media;

namespace DesktopFish.Rendering;

/// <summary>
/// 一条荧光玻璃鱼用到的全部画刷与画笔。所有 Freezable 都在创建后冻结，
/// 可以跨帧、跨鱼复用。配色只由强调色（荧光带）与副强调色（体后红斑、
/// 内脏）决定，体壁本身几乎不着色。
/// </summary>
public sealed class FishPalette
{
    public required Brush BodyFill { get; init; }

    public required Brush FinFill { get; init; }

    public required Pen RimPen { get; init; }

    public required Pen SeamPen { get; init; }

    public required Pen FinLinePen { get; init; }

    public required Brush HaloBrush { get; init; }

    public required Pen GlowWidePen { get; init; }

    public required Pen GlowCorePen { get; init; }

    public required Brush EyeRingFill { get; init; }

    public required Pen EyeRingPen { get; init; }

    public required Brush EyeGlowBrush { get; init; }

    public required Brush EyeCoreBrush { get; init; }

    /// <summary>透出的脊椎、肋骨与鳃弓。</summary>
    public required Pen AnatomyPen { get; init; }

    /// <summary>内脏与鳔，柔边半透明。</summary>
    public required Brush OrganBrush { get; init; }

    /// <summary>荧光带外层的散光。</summary>
    public required Brush FluoroOuterBrush { get; init; }

    /// <summary>荧光带中层的主色。</summary>
    public required Brush FluoroMidBrush { get; init; }

    /// <summary>体后软斑（参考霓虹灯鱼尾部暖色）。</summary>
    public required Brush PatchBrush { get; init; }

    /// <summary>背脊与头部的镜面高光。</summary>
    public required Pen HighlightPen { get; init; }

    internal static FishPalette Create(Color accent, Color secondary)
    {
        return new FishPalette
        {
            BodyFill = BuildBodyBrush(accent),
            FinFill = BuildFinBrush(accent),
            // 亮边上下厚、侧中薄，靠菲涅尔反射读出玻璃的圆度。
            RimPen = GlassRimPen(),
            SeamPen = Pen(Color.FromRgb(0xCF, 0xE8, 0xF2), 0x3C, 0.9),
            FinLinePen = Pen(Color.FromRgb(0xDF, 0xF4, 0xFF), 0x76, 0.85),
            HaloBrush = Halo(accent, 0x38),
            GlowWidePen = Pen(accent, 0x40, 4.4),
            GlowCorePen = Pen(Blend(accent, Colors.White, 0.55), 0xC8, 1.35),
            EyeRingFill = Solid(Color.FromRgb(0x08, 0x14, 0x1C), 0xE8),
            EyeRingPen = Pen(Color.FromRgb(0xE4, 0xF6, 0xFF), 0xD0, 1.1),
            EyeGlowBrush = Solid(accent, 0x50),
            EyeCoreBrush = Solid(Blend(accent, Colors.White, 0.4), 0xFF),
            AnatomyPen = Pen(Color.FromRgb(0xE8, 0xF4, 0xF4), 0x32, 0.75),
            OrganBrush = Patch(Blend(secondary, accent, 0.2), 0x3C),
            FluoroOuterBrush = Solid(accent, 0x16),
            FluoroMidBrush = Solid(accent, 0x52),
            PatchBrush = Patch(secondary, 0x4E),
            HighlightPen = Pen(Colors.White, 0x9E, 2.0),
        };
    }

    private static LinearGradientBrush BuildBodyBrush(Color accent)
    {
        var brush = NewGradient();

        // 体壁很薄但不是零：背部与腹部略厚，正中半透，桌面仍然透得过来。
        brush.GradientStops.Add(new GradientStop(WithAlpha(Blend(accent, Colors.White, 0.55), 0x46), 0.00));
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, 0x2C), 0.22));
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, 0x16), 0.52));
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, 0x26), 0.78));
        brush.GradientStops.Add(new GradientStop(WithAlpha(Blend(accent, Colors.White, 0.4), 0x3E), 1.00));
        brush.Freeze();
        return brush;
    }

    private static LinearGradientBrush BuildFinBrush(Color accent)
    {
        var brush = NewGradient();

        // 薄鳍：根部带体色，向鳍缘渐透，但整体仍有一层可见的膜。
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, 0x74), 0.00));
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, 0x40), 0.42));
        brush.GradientStops.Add(new GradientStop(WithAlpha(accent, 0x16), 1.00));
        brush.Freeze();
        return brush;
    }

    /// <summary>
    /// 玻璃体壁的亮边：上下缘最亮、体侧中部最暗，像光从侧后方擦过圆柱面，
    /// 这是“透明”能读出圆形体积的关键。
    /// </summary>
    private static Pen GlassRimPen()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
        };
        brush.GradientStops.Add(new GradientStop(WithAlpha(Color.FromRgb(0xF0, 0xFB, 0xFF), 0xC8), 0.00));
        brush.GradientStops.Add(new GradientStop(WithAlpha(Color.FromRgb(0xC4, 0xE4, 0xF2), 0x58), 0.20));
        brush.GradientStops.Add(new GradientStop(WithAlpha(Color.FromRgb(0xE8, 0xF8, 0xFF), 0x14), 0.50));
        brush.GradientStops.Add(new GradientStop(WithAlpha(Color.FromRgb(0xC4, 0xE4, 0xF2), 0x58), 0.80));
        brush.GradientStops.Add(new GradientStop(WithAlpha(Color.FromRgb(0xF0, 0xFB, 0xFF), 0xC8), 1.00));
        brush.Freeze();

        var pen = new Pen(brush, 1.15)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        };
        pen.Freeze();
        return pen;
    }

    private static LinearGradientBrush NewGradient()
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
        };
    }

    /// <summary>
    /// 水下光晕：按近似高斯的方式分段衰减，而不是中心直接跳到边缘 0。
    /// 段数多一点，实际尺寸下才是一圈柔和晕开的雾，而不是一块有边界的色斑。
    /// </summary>
    private static RadialGradientBrush Halo(Color color, byte centerAlpha)
    {
        var halo = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.5),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
        };

        (double Offset, double Weight)[] falloff =
        {
            (0.00, 1.00),
            (0.16, 0.74),
            (0.32, 0.48),
            (0.48, 0.28),
            (0.64, 0.14),
            (0.80, 0.05),
            (1.00, 0.00),
        };

        foreach ((double offset, double weight) in falloff)
        {
            halo.GradientStops.Add(new GradientStop(
                WithAlpha(color, (byte)Math.Round(centerAlpha * weight)),
                offset));
        }

        halo.Freeze();
        return halo;
    }

    /// <summary>软斑：中心不透明、边缘渐隐的径向渐变，用来表现内脏与尾部暖斑。</summary>
    private static RadialGradientBrush Patch(Color color, byte centerAlpha)
    {
        var patch = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.5),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
        };
        patch.GradientStops.Add(new GradientStop(WithAlpha(color, centerAlpha), 0.0));
        patch.GradientStops.Add(new GradientStop(WithAlpha(color, (byte)(centerAlpha * 0.55)), 0.6));
        patch.GradientStops.Add(new GradientStop(WithAlpha(color, 0x00), 1.0));
        patch.Freeze();
        return patch;
    }

    private static Color Blend(Color color, Color target, double amount)
    {
        return Color.FromArgb(
            color.A,
            (byte)Math.Clamp(color.R + (target.R - color.R) * amount, 0, 255),
            (byte)Math.Clamp(color.G + (target.G - color.G) * amount, 0, 255),
            (byte)Math.Clamp(color.B + (target.B - color.B) * amount, 0, 255));
    }

    private static Pen Pen(Color color, byte alpha, double thickness)
    {
        var pen = new Pen(Solid(color, alpha), thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        };
        pen.Freeze();
        return pen;
    }

    private static SolidColorBrush Solid(Color color, byte alpha)
    {
        var brush = new SolidColorBrush(WithAlpha(color, alpha));
        brush.Freeze();
        return brush;
    }

    private static Color WithAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
