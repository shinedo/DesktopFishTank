using System;
using System.Windows;
using System.Windows.Media;

namespace DesktopFishTank.Rendering;

/// <summary>
/// 荧光水母：半透明钟形伞盖、伞缘一圈荧光，以及向后拖曳的细长触手
/// 和四条褶边口腕。伞盖随相位做收缩—舒张的搏动，伞缘向伞顶收拢；
/// 触手与口腕各自错相摆动，所有动态几何按相位分档缓存。
/// </summary>
internal sealed class JellyfishShape : FishShape
{
    public override double HaloRadiusX => 76.0;

    public override double HaloRadiusY => 46.0;

    /// <summary>伞盖：钟形，顶点在 +32，伞口在 -11 一带，高约 61。</summary>
    private static readonly Geometry Bell = Build(ctx =>
    {
        ctx.BeginFigure(new Point(32, 0), true, true);
        ctx.BezierTo(new Point(29, -13), new Point(17, -25), new Point(0, -29), true, true);
        ctx.BezierTo(new Point(-5, -30.5), new Point(-9.5, -28), new Point(-10.5, -23), true, true);
        ctx.BezierTo(new Point(-11.5, -12), new Point(-11.5, 12), new Point(-10.5, 23), true, true);
        ctx.BezierTo(new Point(-9.5, 28), new Point(-5, 30.5), new Point(0, 29), true, true);
        ctx.BezierTo(new Point(17, 25), new Point(29, 13), new Point(32, 0), true, true);
    });

    /// <summary>伞缘外沿：一圈自发光，水母的荧光最集中在这里。</summary>
    private static readonly Geometry BellRim = Build(ctx =>
    {
        ctx.BeginFigure(new Point(-10.4, -23), false, false);
        ctx.BezierTo(new Point(-11.0, -8), new Point(-11.0, 8), new Point(-10.4, 23), true, false);
    });

    /// <summary>伞缘内侧：一条稍靠里的淡线，暗示伞壁有厚度。</summary>
    private static readonly Geometry BellInnerRim = Build(ctx =>
    {
        ctx.BeginFigure(new Point(-8.4, -20.5), false, false);
        ctx.BezierTo(new Point(-8.9, -7), new Point(-8.9, 7), new Point(-8.4, 20.5), true, false);
    });

    /// <summary>伞顶的镜面高光。</summary>
    private static readonly Geometry BellHighlight = Build(ctx =>
    {
        ctx.BeginFigure(new Point(28, -6), false, false);
        ctx.BezierTo(new Point(22, -14), new Point(14, -20), new Point(4, -23), true, false);
    });

    /// <summary>触手按 32 档相位缓存。</summary>
    private const int WaveSteps = 32;
    private static readonly Geometry?[] TentacleCache = new Geometry?[WaveSteps];

    /// <summary>口腕的摆动比触手慢，单独用 24 档。</summary>
    private const int ArmSteps = 24;
    private static readonly Geometry?[] OralArmCache = new Geometry?[ArmSteps];

    public override void Draw(DrawingContext dc, FishPalette palette, double phase, double speed, double sheenPhase)
    {
        // 0 = 完全舒张，1 = 收缩到底；伞缘向伞顶收拢，同时伞盖略微变高。
        double pulse = 0.5 + 0.5 * Math.Sin(phase);
        double contract = 0.86 + 0.14 * pulse;

        DrawTentacles(dc, palette, phase);
        DrawOralArms(dc, palette, phase * 0.55);

        dc.PushTransform(new ScaleTransform(contract, 1.0 + 0.05 * (1.0 - pulse), 32, 0));

        PaintBody(dc, palette, Bell);

        dc.PushClip(Bell);

        // 伞腔里的发光团：水母体内的荧光物质，透过伞壁看是一团柔光。
        dc.DrawEllipse(palette.OrganBrush, null, new Point(8, 0), 15.0, 19.0);

        dc.DrawGeometry(null, palette.HighlightPen, BellHighlight);

        // 伞盖表面的折射光带，和鱼身上的扫光一致。
        DrawBodySheen(dc, sheenPhase, -14, 54, -32, 64, -14);

        dc.Pop();

        // 伞缘：宽而淡的散光 + 中心亮线 + 内侧一层薄壁线。
        dc.PushOpacity(0.55);
        dc.DrawGeometry(null, palette.GlowWidePen, BellRim);
        dc.Pop();

        dc.DrawGeometry(null, palette.GlowCorePen, BellRim);
        dc.DrawGeometry(null, palette.SeamPen, BellInnerRim);

        dc.Pop();
    }

    private static void DrawTentacles(DrawingContext dc, FishPalette palette, double phase)
    {
        Geometry geometry = TentacleCache[WaveIndex(phase, WaveSteps)] ??= BuildTentacles(WavePhase(phase, WaveSteps));

        dc.PushOpacity(0.60);
        dc.DrawGeometry(null, palette.GlowWidePen, geometry);
        dc.Pop();

        dc.DrawGeometry(null, palette.SeamPen, geometry);
    }

    private static void DrawOralArms(DrawingContext dc, FishPalette palette, double phase)
    {
        Geometry geometry = OralArmCache[WaveIndex(phase, ArmSteps)] ??= BuildOralArms(WavePhase(phase, ArmSteps));

        // 口腕是薄纱一样的褶边，用更淡的缝线描边、再压一点透明度，
        // 避免读成四根实心管子。
        dc.PushOpacity(0.75);
        PaintFin(dc, palette, geometry, palette.SeamPen);
        dc.Pop();
    }

    private static Geometry BuildTentacles(double phase)
    {
        return Build(ctx =>
        {
            for (int i = 0; i < 9; i++)
            {
                double rootY = -23 + i * (46.0 / 8.0);
                double swing = Math.Sin(phase + i * 0.8) * 7.0;
                double length = 40.0 + 10.0 * Math.Sin(phase * 0.7 + i * 1.7);

                // 触手根部落在伞缘上，被伞缘那一圈荧光盖住；
                // 放在伞盖内部会露出一排圆头，看起来像一排齿。
                const double rootX = -9.0;

                ctx.BeginFigure(new Point(rootX, rootY), false, false);
                ctx.BezierTo(
                    new Point(rootX - length * 0.28, rootY + swing * 0.25),
                    new Point(rootX - length * 0.65, rootY * 1.10 + swing),
                    new Point(rootX - length, rootY * 1.25 + swing * 1.5),
                    true,
                    false);
            }
        });
    }

    /// <summary>四条褶边口腕：从伞口向后垂下，比触手粗，末端带皱褶。</summary>
    private static Geometry BuildOralArms(double phase)
    {
        return Build(ctx =>
        {
            for (int i = 0; i < 4; i++)
            {
                double rootY = -7.0 + i * 4.7;
                double swing = Math.Sin(phase * 1.1 + i * 1.3) * 5.0;
                double length = 30.0 + i * 3.0;

                // 口腕根部收在伞腔里，任何搏动相位下都被伞盖盖住，不会露出断口。
                const double rootX = -4.0;

                // 带状口腕从伞口向后垂下：上缘走到尖端，下缘再收回根部。
                // 单向前进的闭合路径不会自交，填充才是一整条柔软的褶边。
                ctx.BeginFigure(new Point(rootX, rootY - 1.8), true, true);
                ctx.BezierTo(
                    new Point(-15, rootY - 2.8),
                    new Point(-23, rootY * 1.15 - 2.2 + swing * 0.6),
                    new Point(rootX - length, rootY * 1.5 + swing * 1.2),
                    true,
                    true);
                ctx.BezierTo(
                    new Point(-23, rootY * 1.15 + 2.0 + swing * 0.6),
                    new Point(-15, rootY + 2.4),
                    new Point(rootX, rootY + 1.8),
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
}
