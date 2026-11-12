using System;
using System.Windows;
using System.Windows.Media;

namespace DesktopFishTank.Rendering;

/// <summary>
/// 荧光磷虾：半透明头胸甲、分节并向后下弯的腹部、扇形尾扇、一对长触须、
/// 五对胸足与五对游泳足。造型取南极磷虾的解剖特征——甲壳薄而透，
/// 肝胰腺与消化道能看见，眼柄把复眼顶出甲壳外，尾扇由中央尾节与两对尾肢组成。
/// 全部矢量绘制，摆动几何按相位分档缓存，鱼多时不增加每帧分配。
/// </summary>
internal sealed class KrillShape : FishShape
{
    public override double HaloRadiusX => 64.0;

    public override double HaloRadiusY => 30.0;

    /// <summary>头胸甲：吻端在 +43，后缘收到 -7，背缘略拱。</summary>
    private static readonly Geometry Carapace = Build(ctx =>
    {
        ctx.BeginFigure(new Point(43, 0), true, true);
        ctx.BezierTo(new Point(37, -7.5), new Point(25, -13), new Point(10, -14), true, true);
        ctx.BezierTo(new Point(0, -14.5), new Point(-7, -11), new Point(-7, -5), true, true);
        ctx.BezierTo(new Point(-7, 2), new Point(2, 9.5), new Point(16, 11.5), true, true);
        ctx.BezierTo(new Point(27, 12.8), new Point(37, 8), new Point(43, 0), true, true);
    });

    /// <summary>腹部：六节，越往后越细，并向下方弯。</summary>
    private static readonly Geometry Abdomen = Build(ctx =>
    {
        ctx.BeginFigure(new Point(-5, -6.2), true, true);
        ctx.BezierTo(new Point(-13, -5.2), new Point(-22, -2.2), new Point(-31, 2.6), true, true);
        ctx.BezierTo(new Point(-36, 5.2), new Point(-40, 7.0), new Point(-43.5, 8.2), true, true);
        ctx.QuadraticBezierTo(new Point(-45.5, 9.2), new Point(-44.2, 10.8), true, true);
        ctx.BezierTo(new Point(-39, 10.2), new Point(-32, 8.2), new Point(-24, 6.4), true, true);
        ctx.BezierTo(new Point(-16, 4.6), new Point(-9, 3.2), new Point(-5, 3.0), true, true);
        ctx.QuadraticBezierTo(new Point(-4.4, -1.6), new Point(-5, -6.2), true, true);
    });

    /// <summary>腹部体节：五道横跨腹部的浅弧，是磷虾最容易辨认的节段感。</summary>
    private static readonly Geometry AbdomenSegments = Build(ctx =>
    {
        for (int i = 1; i <= 5; i++)
        {
            double u = i / 6.0;
            double xTop = -5 - 38.5 * u;
            double xBot = -5 - 39.0 * u;
            double yTop = -6.2 + 14.4 * Math.Pow(u, 1.6);
            double yBot = 3.0 + 7.8 * Math.Pow(u, 1.15);

            ctx.BeginFigure(new Point(xTop, yTop), false, false);
            ctx.QuadraticBezierTo(
                new Point((xTop + xBot) * 0.5 - 1.8, (yTop + yBot) * 0.5 + 0.8),
                new Point(xBot, yBot),
                true,
                false);
        }
    });

    /// <summary>尾扇：中央尾节 + 上、下两对尾肢，展开成扇形。</summary>
    private static readonly Geometry TailFan = Build(ctx =>
    {
        // 中央尾节
        ctx.BeginFigure(new Point(-42.5, 8.6), true, true);
        ctx.BezierTo(new Point(-50, 8), new Point(-58, 8.2), new Point(-62, 9.4), true, true);
        ctx.QuadraticBezierTo(new Point(-60, 12.6), new Point(-54, 13.2), true, true);
        ctx.BezierTo(new Point(-49, 13.4), new Point(-45, 11.8), new Point(-42.5, 10.6), true, true);

        // 上尾肢
        ctx.BeginFigure(new Point(-42, 7.6), true, true);
        ctx.BezierTo(new Point(-49, 4), new Point(-56, 0.6), new Point(-61, -1.0), true, true);
        ctx.BezierTo(new Point(-58, 3), new Point(-52, 6.4), new Point(-45.5, 9.2), true, true);

        // 下尾肢
        ctx.BeginFigure(new Point(-42.2, 10.4), true, true);
        ctx.BezierTo(new Point(-49, 12.8), new Point(-56, 16), new Point(-61, 18.2), true, true);
        ctx.BezierTo(new Point(-57, 14.4), new Point(-51, 12.6), new Point(-45.5, 11.4), true, true);
    });

    /// <summary>体侧荧光带：从眼后沿甲壳后缘一直贯穿到尾柄。</summary>
    private static readonly Geometry GlowStripe = Build(ctx =>
    {
        ctx.BeginFigure(new Point(32, -3.0), true, true);
        ctx.BezierTo(new Point(18, -6.0), new Point(0, -4.0), new Point(-12, 0.5), true, true);
        ctx.BezierTo(new Point(-22, 3.4), new Point(-32, 6.0), new Point(-40, 7.6), true, true);
        ctx.BezierTo(new Point(-30, 6.6), new Point(-20, 4.6), new Point(-10, 2.8), true, true);
        ctx.BezierTo(new Point(2, 1.2), new Point(18, 0.8), new Point(32, -3.0), true, true);
    });

    private static readonly Geometry GlowCore = Build(ctx =>
    {
        ctx.BeginFigure(new Point(28, -1.6), false, false);
        ctx.BezierTo(new Point(14, -2.0), new Point(0, -0.6), new Point(-13, 2.4), true, false);
        ctx.BezierTo(new Point(-21, 4.2), new Point(-28, 5.6), new Point(-34, 6.4), true, false);
    });

    /// <summary>肝胰腺：头胸甲后半部的一团柔色腺体，磷虾透明的甲壳下最显眼的内部结构。</summary>
    private static readonly Geometry Hepatopancreas = Build(ctx =>
    {
        ctx.BeginFigure(new Point(30, -2), true, true);
        ctx.BezierTo(new Point(22, 6), new Point(8, 8), new Point(-3, 4), true, true);
        ctx.BezierTo(new Point(2, -4), new Point(16, -10), new Point(30, -2), true, true);
    });

    /// <summary>消化道：从胃沿腹部一直通到尾柄。</summary>
    private static readonly Geometry DigestiveTract = Build(ctx =>
    {
        ctx.BeginFigure(new Point(26, -1), false, false);
        ctx.BezierTo(new Point(12, 2.5), new Point(-8, 3.5), new Point(-26, 6.0), true, false);
        ctx.QuadraticBezierTo(new Point(-34, 7.2), new Point(-40, 8.2), true, false);
    });

    /// <summary>头胸甲上缘的镜面高光。</summary>
    private static readonly Geometry CarapaceHighlight = Build(ctx =>
    {
        ctx.BeginFigure(new Point(36, -4.8), false, false);
        ctx.BezierTo(new Point(30, -9.6), new Point(24, -11.6), new Point(17, -12.2), true, false);
    });

    private static readonly Brush Heart =
        FrozenSolid(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF));

    private static readonly Brush EyeHighlight =
        FrozenSolid(Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF));

    /// <summary>触须按 32 档相位缓存，避免每条磷虾每帧重建几何。</summary>
    private const int WaveSteps = 32;
    private static readonly Geometry?[] AntennaCache = new Geometry?[WaveSteps];

    /// <summary>胸足与腹足的摆动各自按 24 档相位缓存。</summary>
    private const int LegSteps = 24;
    private static readonly Geometry?[] ThoracopodCache = new Geometry?[LegSteps];
    private static readonly Geometry?[] PleopodCache = new Geometry?[LegSteps];

    public override void Draw(DrawingContext dc, FishPalette palette, double phase, double speed)
    {
        // 磷虾的附肢频率明显高于鱼鳍，同一个相位乘一个系数就够。
        double p = phase * 1.45;

        DrawTailFan(dc, palette, p, speed);
        DrawPleopods(dc, palette, p);
        DrawThoracopods(dc, palette, p);
        DrawAntennae(dc, palette, p);

        PaintBody(dc, palette, Abdomen);
        PaintBody(dc, palette, Carapace);

        // 甲壳内的肝胰腺与消化道：透明甲壳下若隐若现的内部结构。
        dc.PushClip(Carapace);
        dc.DrawGeometry(palette.OrganBrush, palette.SeamPen, Hepatopancreas);
        dc.DrawEllipse(Heart, null, new Point(10, -6.5), 2.0, 1.6);
        dc.Pop();

        dc.PushClip(Abdomen);
        dc.DrawGeometry(null, palette.AnatomyPen, DigestiveTract);
        dc.DrawGeometry(null, palette.SeamPen, AbdomenSegments);
        dc.Pop();

        DrawGlowStripe(dc, palette, p);

        // 额剑：吻端向前上方的一根尖刺。
        dc.DrawLine(palette.HighlightPen, new Point(42, -0.5), new Point(52, -4.8));

        // 头胸甲高光。
        dc.DrawGeometry(null, palette.HighlightPen, CarapaceHighlight);

        // 眼柄把复眼顶出甲壳外，是磷虾最醒目的头部特征。
        dc.DrawLine(palette.RimPen, new Point(35, -8.5), new Point(38.5, -13.4));
        DrawEye(dc, palette, new Point(40.2, -15.4), 4.0, 3.0, 1.5);
        dc.DrawEllipse(EyeHighlight, null, new Point(41.1, -16.5), 0.72, 0.72);
    }

    private static void DrawTailFan(DrawingContext dc, FishPalette palette, double phase, double speed)
    {
        // 尾扇整体绕尾柄摆动：摆幅随速度增大，慢游时几乎只是轻轻展开。
        double vigor = Math.Clamp(0.55 + speed / 240.0, 0.55, 1.20);
        double angle = Math.Sin(phase) * (7.0 + vigor * 6.0);

        dc.PushTransform(new RotateTransform(angle, -42, 9));

        dc.PushOpacity(0.30);
        dc.DrawGeometry(null, palette.GlowWidePen, TailFan);
        dc.Pop();

        PaintFin(dc, palette, TailFan, palette.RimPen);
        dc.Pop();
    }

    private static void DrawAntennae(DrawingContext dc, FishPalette palette, double phase)
    {
        Geometry geometry = AntennaCache[WaveIndex(phase, WaveSteps)] ??= BuildAntennae(WavePhase(phase, WaveSteps));

        // 先画一层宽而淡的光，再压一根细线，长触须才有水中的发光感。
        dc.PushOpacity(0.55);
        dc.DrawGeometry(null, palette.GlowWidePen, geometry);
        dc.Pop();

        dc.DrawGeometry(null, palette.SeamPen, geometry);
    }

    private static void DrawThoracopods(DrawingContext dc, FishPalette palette, double phase)
    {
        Geometry geometry = ThoracopodCache[WaveIndex(phase, LegSteps)] ??= BuildThoracopods(WavePhase(phase, LegSteps));
        dc.DrawGeometry(null, palette.AnatomyPen, geometry);
    }

    private static void DrawPleopods(DrawingContext dc, FishPalette palette, double phase)
    {
        Geometry geometry = PleopodCache[WaveIndex(phase, LegSteps)] ??= BuildPleopods(WavePhase(phase, LegSteps));
        PaintFin(dc, palette, geometry, palette.FinLinePen);
    }

    private static void DrawGlowStripe(DrawingContext dc, FishPalette palette, double phase)
    {
        double pulse = 0.86 + 0.14 * Math.Sin(phase * 0.6);
        dc.PushOpacity(pulse);

        dc.DrawGeometry(palette.FluoroOuterBrush, null, GlowStripe);
        dc.DrawGeometry(palette.FluoroMidBrush, null, GlowStripe);
        dc.DrawGeometry(null, palette.GlowCorePen, GlowCore);

        dc.Pop();
    }

    private static Geometry BuildAntennae(double phase)
    {
        double upper = Math.Sin(phase) * 5.0;
        double lower = Math.Sin(phase + 1.9) * 4.2;

        return Build(ctx =>
        {
            // 上触须：很长，向前上方甩出去。
            ctx.BeginFigure(new Point(40, -4.5), false, false);
            ctx.BezierTo(
                new Point(55, -9 + upper * 0.3),
                new Point(70, -15 + upper),
                new Point(86, -13 + upper * 1.4),
                true,
                false);

            // 下触须：略短，向前下方扫。
            ctx.BeginFigure(new Point(40, -2.0), false, false);
            ctx.BezierTo(
                new Point(56, 3 + lower * 0.3),
                new Point(72, 7 + lower),
                new Point(88, 6 + lower * 1.2),
                true,
                false);

            // 小触角：短的双叉。
            ctx.BeginFigure(new Point(39, -5.5), false, false);
            ctx.QuadraticBezierTo(new Point(46, -10), new Point(52, -9 + upper * 0.5), true, false);
            ctx.BeginFigure(new Point(39, -4.5), false, false);
            ctx.QuadraticBezierTo(new Point(45, -2), new Point(50, -1 + upper * 0.4), true, false);
        });
    }

    /// <summary>五对胸足：从甲壳下缘向后下方伸出，逐对错相摆动。</summary>
    private static Geometry BuildThoracopods(double phase)
    {
        return Build(ctx =>
        {
            for (int i = 0; i < 5; i++)
            {
                double rootX = 30 - i * 5.5;
                double rootY = 9.5 - i * 0.4;
                double swing = Math.Sin(phase + i * 0.7) * 2.6;

                ctx.BeginFigure(new Point(rootX, rootY), false, false);
                ctx.LineTo(new Point(rootX - 1.5 + swing * 0.3, rootY + 5.5), true, false);
                ctx.LineTo(new Point(rootX - 4.5 + swing, rootY + 10.5), true, false);
            }
        });
    }

    /// <summary>五对游泳足：腹部下方的小桨叶，摆动幅度比胸足大。</summary>
    private static Geometry BuildPleopods(double phase)
    {
        return Build(ctx =>
        {
            for (int i = 0; i < 5; i++)
            {
                double rootX = -10 - i * 6.4;
                double rootY = 5.4 + i * 0.7;
                double swing = Math.Sin(phase * 1.25 + i) * 4.2;

                ctx.BeginFigure(new Point(rootX, rootY), true, true);
                ctx.BezierTo(
                    new Point(rootX - 1.5, rootY + 3.6),
                    new Point(rootX - 6.0 + swing, rootY + 5.6),
                    new Point(rootX - 9.5 + swing, rootY + 3.2),
                    true,
                    true);
                ctx.BezierTo(
                    new Point(rootX - 6.0 + swing * 0.6, rootY + 1.6),
                    new Point(rootX - 3.0, rootY + 0.6),
                    new Point(rootX, rootY),
                    true,
                    true);
            }
        });
    }

    private static int WaveIndex(double phase, int steps)
    {
        double normalized = phase % (Math.PI * 2);
        if (normalized < 0)
        {
            normalized += Math.PI * 2;
        }

        return (int)(normalized / (Math.PI * 2) * steps) % steps;
    }

    private static double WavePhase(double phase, int steps)
    {
        return (WaveIndex(phase, steps) + 0.5) / steps * Math.PI * 2;
    }

    private static Brush FrozenSolid(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
