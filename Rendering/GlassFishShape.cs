using System;
using System.Windows;
using System.Windows.Media;

namespace DesktopFishTank.Rendering;

/// <summary>
/// 荧光玻璃鱼：半透明鱼体、透出的骨骼与内脏、体侧荧光带。
/// 造型参考玻璃猫鱼与霓虹灯鱼——透明与发光都取真实鱼类的光学特征：
/// 体壁几乎不着色，靠菲涅尔亮边勾出形体，肋骨与脊椎透过体壁可见；
/// 荧光带是自发光色带，用多层叠色模拟，不做高斯模糊。
/// 全部由矢量绘制，不依赖位图素材。
/// </summary>
internal sealed class GlassFishShape : FishShape
{
    public override double HaloRadiusX => 102.0;

    public override double HaloRadiusY => 50.0;

    /// <summary>躯干：纺锤形，背略拱、腹略垂，尾柄收得很细。</summary>
    private static readonly Geometry Body = Build(ctx =>
    {
        ctx.BeginFigure(new Point(46, 1), true, true);
        ctx.BezierTo(new Point(44, -6), new Point(39, -12), new Point(31, -15), true, true);
        ctx.BezierTo(new Point(16, -18), new Point(-2, -18), new Point(-18, -13.5), true, true);
        ctx.BezierTo(new Point(-30, -9.5), new Point(-39, -4), new Point(-44, -2.2), true, true);
        ctx.QuadraticBezierTo(new Point(-45, 0), new Point(-44, 2.2), true, true);
        ctx.BezierTo(new Point(-36, 5.5), new Point(-24, 11), new Point(-10, 14.5), true, true);
        ctx.BezierTo(new Point(6, 17), new Point(23, 14), new Point(33, 9), true, true);
        ctx.BezierTo(new Point(40, 5.5), new Point(44.5, 3), new Point(46, 1), true, true);
    });

    private static readonly Geometry DorsalFin = Build(ctx =>
    {
        ctx.BeginFigure(new Point(20, -15.0), true, true);
        ctx.BezierTo(new Point(16, -22), new Point(10, -28), new Point(2, -27.5), true, true);
        ctx.BezierTo(new Point(-5, -27), new Point(-11, -20.5), new Point(-15, -15.5), true, true);
    });

    /// <summary>臀鳍：沿腹部向后拉得很长，是玻璃猫鱼的标志性轮廓。</summary>
    private static readonly Geometry AnalFin = Build(ctx =>
    {
        ctx.BeginFigure(new Point(-3, 14.5), true, true);
        ctx.BezierTo(new Point(-10, 21.5), new Point(-21, 24), new Point(-30, 18.5), true, true);
        ctx.BezierTo(new Point(-36, 14), new Point(-40, 8), new Point(-42.5, 3.5), true, true);
    });

    private static readonly Geometry PelvicFin = Build(ctx =>
    {
        ctx.BeginFigure(new Point(-3, 13.5), true, true);
        ctx.BezierTo(new Point(-6.5, 18), new Point(-11, 20.5), new Point(-15, 18.5), true, true);
        ctx.BezierTo(new Point(-12, 15), new Point(-7.5, 13.5), new Point(-3, 13.5), true, true);
    });

    private static readonly Geometry PectoralFin = Build(ctx =>
    {
        ctx.BeginFigure(new Point(31, 4), true, true);
        ctx.BezierTo(new Point(26, 10), new Point(18, 15), new Point(10, 14), true, true);
        ctx.BezierTo(new Point(18, 10.5), new Point(25.5, 7), new Point(31, 4), true, true);
    });

    /// <summary>脊椎：一节节椎骨，比一条连续直线更像真实骨骼。</summary>
    private static readonly Geometry Spine = Build(ctx =>
    {
        const int vertebrae = 20;
        for (int i = 0; i < vertebrae; i++)
        {
            double t = (double)i / (vertebrae - 1);
            double x = 39 - t * 80;
            double y = -1.4 + Math.Sin(t * Math.PI) * -0.7;
            double half = 1.25 + Math.Sin(t * Math.PI) * 0.95;
            ctx.BeginFigure(new Point(x, y - half), false, false);
            ctx.LineTo(new Point(x - 0.9, y + half), true, false);
        }
    });

    /// <summary>肋骨：近侧向后下方弯得长，远侧更短更直，两侧叠出体内纵深。</summary>
    private static readonly Geometry Ribs = Build(ctx =>
    {
        // 肋骨只画得出一层若隐若现的影子，画满整副胸廓会变成 X 光片。
        const int pairs = 9;
        for (int i = 0; i < pairs; i++)
        {
            double t = (double)i / (pairs - 1);
            double x = 26 - t * 52;
            double length = 3.6 + Math.Sin(Math.PI * (0.12 + t * 0.82)) * 4.4;

            ctx.BeginFigure(new Point(x, -1.2), false, false);
            ctx.QuadraticBezierTo(
                new Point(x - 2.4, -1.2 + length * 0.72),
                new Point(x - 6.2, -1.2 + length),
                true,
                false);

            ctx.BeginFigure(new Point(x - 1.3, -1.2), false, false);
            ctx.QuadraticBezierTo(
                new Point(x - 3.4, -1.2 - length * 0.42),
                new Point(x - 6.6, -1.2 - length * 0.58),
                true,
                false);
        }
    });

    private static readonly Geometry GillArches = Build(ctx =>
    {
        for (int i = 0; i < 3; i++)
        {
            double x = 34.5 - i * 2.1;
            ctx.BeginFigure(new Point(x, -8.5), false, false);
            ctx.QuadraticBezierTo(new Point(x - 3.6, 0), new Point(x - 1.0, 8.2), true, false);
        }
    });

    private static readonly Geometry Gut = Build(ctx =>
    {
        ctx.BeginFigure(new Point(28, 4.6), false, false);
        ctx.BezierTo(new Point(18, 8), new Point(6, 9.8), new Point(-6, 9.2), true, false);
        ctx.BezierTo(new Point(-16, 8.8), new Point(-22, 7.2), new Point(-28, 5.4), true, false);
    });

    private static readonly Geometry SwimBladder =
        FrozenGeometry(new EllipseGeometry(new Point(9, -6.5), 14.0, 3.6));

    /// <summary>体侧荧光带中心线：从眼后一直贯穿到尾柄。</summary>
    private static readonly Geometry StripeCenter = Build(ctx =>
    {
        ctx.BeginFigure(new Point(33, -3.0), false, false);
        ctx.BezierTo(new Point(20, -7.2), new Point(-20, -6.4), new Point(-42, -1.8), true, false);
    });

    private static readonly Geometry StripeOuter = BuildStripe(3.2);
    private static readonly Geometry StripeMid = BuildStripe(1.7);

    /// <summary>背部镜面高光：玻璃体表面那道又长又亮的水光。</summary>
    private static readonly Geometry BackHighlight = Build(ctx =>
    {
        ctx.BeginFigure(new Point(34, -9.0), false, false);
        ctx.BezierTo(new Point(16, -12.6), new Point(0, -13.2), new Point(-20, -10.4), true, false);
    });

    private static readonly Geometry HeadHighlight = Build(ctx =>
    {
        ctx.BeginFigure(new Point(41.5, -2.2), false, false);
        ctx.BezierTo(new Point(39, -6.6), new Point(35, -8.6), new Point(31, -9.2), true, false);
    });

    private static readonly Geometry Mouth = Build(ctx =>
    {
        ctx.BeginFigure(new Point(44.5, 1.6), false, false);
        ctx.QuadraticBezierTo(new Point(43.4, 3.4), new Point(41.6, 3.6), true, false);
    });

    /// <summary>鳃盖：透明鱼的头部比躯干实一些，一道鳃盖弧就有解剖感。</summary>
    private static readonly Geometry Operculum = Build(ctx =>
    {
        ctx.BeginFigure(new Point(34.5, -10.5), false, false);
        ctx.BezierTo(new Point(31.0, -5.0), new Point(30.4, 1.0), new Point(32.0, 7.8), true, false);
    });

    /// <summary>头部微光：头骨区比体侧厚，压一层淡色，不会整条鱼一样透。</summary>
    private static readonly Brush HeadWash =
        new SolidColorBrush(Color.FromArgb(0x1E, 0xDF, 0xF4, 0xFF));

    /// <summary>眼窝阴影：把眼球按进头里，而不是让它浮在体表。</summary>
    private static readonly Brush EyeSocketShade = CreateEyeSocket();

    /// <summary>眼睛的两点高光，静态复用，避免每帧、每条鱼新建画刷。</summary>
    private static readonly Brush EyeHighlight = FrozenSolid(Color.FromArgb(0xDC, 0xFF, 0xFF, 0xFF));

    private static readonly Brush EyeHighlightSoft = FrozenSolid(Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF));

    /// <summary>尾鳍几何缓存：48 档相位 × 4 档摆幅，按需构建并冻结。</summary>
    private const int TailPhaseSteps = 48;
    private const int TailAmplitudeSteps = 4;
    private static readonly Geometry?[] TailCache = new Geometry?[TailPhaseSteps * TailAmplitudeSteps];

    /// <summary>扫过体侧的柔光带，随相位横向移动，模拟玻璃折射的光泽。</summary>
    private static readonly Brush SheenBrush = CreateSheen();

    /// <summary>背光：体壁上方的一层柔亮，给扁平的轮廓一点圆润的体积。</summary>
    private static readonly Brush BackLight = CreateBodyShade(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF));

    /// <summary>腹影：腹部压一层冷暗色，玻璃才有厚度，而不是一张薄膜。</summary>
    private static readonly Brush BellyShade = CreateBodyShade(Color.FromArgb(0x2C, 0x05, 0x10, 0x1C));

    public override void Draw(DrawingContext dc, FishPalette palette, double phase, double speed)
    {
        double dorsalSway = Math.Sin(phase + 1.1) * 5.0;
        double pectoralSway = Math.Sin(phase + 0.4) * 15.0;
        double pelvicSway = Math.Sin(phase + 1.9) * 9.0;

        DrawTail(dc, palette, phase, speed);

        PaintSwayFin(dc, palette, DorsalFin, palette.FinLinePen, -dorsalSway, 8, -20);
        PaintSwayFin(dc, palette, AnalFin, palette.FinLinePen, dorsalSway * 0.6, -26, 12);
        PaintSwayFin(dc, palette, PelvicFin, palette.FinLinePen, pelvicSway, -6, 15);

        // 身体本身几乎不着色，形体靠亮边与体内结构读出来。
        PaintBody(dc, palette, Body);

        dc.PushClip(Body);

        // 体壁的体积：上半身接光，下半身沉暗，中间才透。
        dc.DrawRectangle(BackLight, null, new Rect(-48, -22, 96, 22));
        dc.DrawRectangle(BellyShade, null, new Rect(-48, 2, 96, 18));

        // 头部比躯干更实：头骨与鳃盖让透明感有对比，而不是一条均匀的玻璃管。
        dc.DrawEllipse(HeadWash, null, new Point(35.5, -2.5), 10.5, 12.5);

        // 腹部反射光：玻璃壁从下方接住的一点水光。
        dc.DrawEllipse(
            palette.OrganBrush,
            null,
            new Point(2, 10.5),
            26.0,
            3.4);

        DrawAnatomy(dc, palette, phase);

        // 折射光带：一条很淡的斜向白光缓慢扫过体侧。
        double sweep = ((phase * 0.13) % 1.0) * 118.0 - 59.0;
        dc.PushTransform(new RotateTransform(-12, 0, 0));
        dc.DrawRectangle(SheenBrush, null, new Rect(sweep, -22, 15, 44));
        dc.Pop();

        // 体后红斑：霓虹灯鱼尾部那段暖色，透在体壁里。
        dc.DrawEllipse(palette.PatchBrush, null, new Point(-16, 5.0), 17.0, 4.8);

        dc.Pop();

        // 近侧体壁的内缘：稍微缩一圈的淡线，暗示体壁有厚度。
        dc.PushTransform(new ScaleTransform(0.965, 0.94));
        dc.DrawGeometry(null, palette.SeamPen, Body);
        dc.Pop();

        DrawFluoroStripe(dc, palette, phase);
        DrawSpecular(dc, palette);

        PaintSwayFin(dc, palette, PectoralFin, palette.FinLinePen, pectoralSway, 30, 5);

        var eyeCenter = new Point(37, -3.6);
        dc.DrawEllipse(EyeSocketShade, null, new Point(36.6, -3.0), 7.6, 7.0);
        DrawEye(dc, palette, eyeCenter, 7.0, 4.6, 2.4);

        // 主高光偏左上，副高光在对角，玻璃球的湿润感来自这两个点。
        dc.DrawEllipse(
            EyeHighlight,
            null,
            new Point(eyeCenter.X + 0.9, eyeCenter.Y - 1.5),
            0.95,
            0.95);
        dc.DrawEllipse(
            EyeHighlightSoft,
            null,
            new Point(eyeCenter.X - 1.1, eyeCenter.Y + 0.9),
            0.42,
            0.42);

        dc.DrawGeometry(null, palette.SeamPen, Mouth);
    }

    /// <summary>体内结构：略微滞后于身体摆动，透明鱼的内脏才有“浮在体腔里”的错觉。</summary>
    private static void DrawAnatomy(DrawingContext dc, FishPalette palette, double phase)
    {
        double lag = Math.Sin(phase * 0.8) * 0.9;
        dc.PushTransform(new TranslateTransform(lag, lag * 0.35));

        // 鳔：半透明的气囊，玻璃鱼体侧最明显的一团内部结构。
        dc.DrawGeometry(palette.OrganBrush, palette.SeamPen, SwimBladder);

        dc.DrawGeometry(null, palette.AnatomyPen, Spine);
        dc.DrawGeometry(null, palette.AnatomyPen, Ribs);
        dc.DrawGeometry(null, palette.AnatomyPen, GillArches);
        dc.DrawGeometry(null, palette.SeamPen, Operculum);
        dc.DrawGeometry(null, palette.SeamPen, Gut);

        dc.Pop();
    }

    /// <summary>荧光带：外层散光 → 中层色带 → 中心亮线，三层叠出类似自发光的亮度。</summary>
    private static void DrawFluoroStripe(DrawingContext dc, FishPalette palette, double phase)
    {
        // 微微呼吸的脉动，避免发光带像一条静止的贴纸。
        double pulse = 0.86 + 0.14 * Math.Sin(phase * 0.6);
        dc.PushOpacity(pulse);

        dc.DrawGeometry(palette.FluoroOuterBrush, null, StripeOuter);
        dc.DrawGeometry(palette.FluoroMidBrush, null, StripeMid);
        dc.DrawGeometry(null, palette.GlowCorePen, StripeCenter);

        dc.Pop();
    }

    /// <summary>表面高光：背部一道长光、头部一道短光，玻璃的湿润感全靠它们。</summary>
    private static void DrawSpecular(DrawingContext dc, FishPalette palette)
    {
        dc.DrawGeometry(null, palette.HighlightPen, BackHighlight);
        dc.DrawGeometry(null, palette.HighlightPen, HeadHighlight);
    }

    /// <summary>叉形尾鳍：透明薄鳍，根部带体色，边缘发荧光，鳍条随游姿摆动。</summary>
    private static void DrawTail(DrawingContext dc, FishPalette palette, double phase, double speed)
    {
        // 尾鳍形状只由相位和摆幅决定。按 48 档相位 × 4 档摆幅缓存，
        // 避免每条鱼每帧都重建 StreamGeometry；30 FPS 下档位小于一帧的相位步长，
        // 看不出阶梯感，但能把每帧的几何分配从「鱼数」降到 0。
        double vigor = Math.Clamp(0.55 + speed / 220.0, 0.55, 1.25);
        int amplitudeBucket = Math.Clamp(
            (int)Math.Round((vigor - 0.55) / 0.7 * (TailAmplitudeSteps - 1)),
            0,
            TailAmplitudeSteps - 1);

        double normalized = phase % (Math.PI * 2);
        if (normalized < 0)
        {
            normalized += Math.PI * 2;
        }

        int phaseBucket = (int)(normalized / (Math.PI * 2) * TailPhaseSteps) % TailPhaseSteps;
        int cacheIndex = amplitudeBucket * TailPhaseSteps + phaseBucket;

        Geometry geometry = TailCache[cacheIndex] ??= BuildTail(
            (phaseBucket + 0.5) / TailPhaseSteps * Math.PI * 2,
            6.5 * (0.55 + amplitudeBucket / (double)(TailAmplitudeSteps - 1) * 0.7));

        dc.PushOpacity(0.28);
        dc.DrawGeometry(null, palette.GlowWidePen, geometry);
        dc.Pop();

        // 尾鳍只保留膜的渐变与轮廓，不画鳍条线——透明鱼尾是一整片薄纱，
        // 加线条会立刻读成“线框”，失掉玻璃的通透感。
        PaintFin(dc, palette, geometry, palette.RimPen);
    }

    private static Geometry BuildTail(double phase, double amplitude)
    {
        double WaveAt(double t) => Math.Sin(phase - t * 1.7) * amplitude * (0.25 + t * 1.15);

        const double rootX = -43.5;
        const double length = 47.0;
        const double spread = 16.5;
        double tipX = rootX - length;

        var geometry = new StreamGeometry();
        using (StreamGeometryContext ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(rootX, -2.4), true, true);
            ctx.BezierTo(
                new Point(rootX - length * 0.35, -spread * 0.55 + WaveAt(0.35)),
                new Point(tipX + length * 0.18, -spread * 0.92 + WaveAt(0.85)),
                new Point(tipX, -spread + WaveAt(1.0)),
                true,
                true);
            ctx.QuadraticBezierTo(
                new Point(rootX - length * 0.60, -spread * 0.10 + WaveAt(0.62)),
                new Point(rootX - length * 0.55, WaveAt(0.55)),
                true,
                true);
            ctx.QuadraticBezierTo(
                new Point(rootX - length * 0.60, spread * 0.10 + WaveAt(0.62)),
                new Point(tipX, spread + WaveAt(1.0)),
                true,
                true);
            ctx.BezierTo(
                new Point(tipX + length * 0.18, spread * 0.92 + WaveAt(0.85)),
                new Point(rootX - length * 0.35, spread * 0.55 + WaveAt(0.35)),
                new Point(rootX, 2.4),
                true,
                true);
        }

        geometry.Freeze();
        return geometry;
    }

    private static Geometry BuildStripe(double half)
    {
        double topMid = -6.8 - half * 1.4;
        return Build(ctx =>
        {
            ctx.BeginFigure(new Point(33, -3.0), true, true);
            ctx.BezierTo(
                new Point(20, topMid),
                new Point(-20, topMid - 0.4),
                new Point(-42, -1.8),
                true,
                true);
            ctx.BezierTo(
                new Point(-20, -1.4 + half),
                new Point(20, -1.2 + half),
                new Point(33, -3.0),
                true,
                true);
        });
    }

    private static Brush FrozenSolid(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    /// <summary>眼窝：中心略暗、边缘透明，避免出现一圈硬边。</summary>
    private static Brush CreateEyeSocket()
    {
        var shade = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.5),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
        };
        shade.GradientStops.Add(new GradientStop(Color.FromArgb(0x30, 0x04, 0x0C, 0x14), 0.00));
        shade.GradientStops.Add(new GradientStop(Color.FromArgb(0x18, 0x04, 0x0C, 0x14), 0.55));
        shade.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x04, 0x0C, 0x14), 1.00));
        shade.Freeze();
        return shade;
    }

    /// <summary>竖直方向的单端渐变，用来给体壁压一层柔和的亮部或暗部。</summary>
    private static Brush CreateBodyShade(Color color)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
        };
        brush.GradientStops.Add(new GradientStop(color, 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, color.R, color.G, color.B), 1.0));
        brush.Freeze();
        return brush;
    }

    private static Brush CreateSheen()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.00));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF), 0.45));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF), 0.55));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 1.00));
        brush.Freeze();
        return brush;
    }

    private static Geometry FrozenGeometry(Geometry geometry)
    {
        geometry.Freeze();
        return geometry;
    }
}
