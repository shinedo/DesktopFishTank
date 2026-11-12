using System;
using System.Windows;
using System.Windows.Media;

namespace DesktopFishTank.Rendering;

/// <summary>
/// 荧光河豚：浑圆的身体、布满体表的短刺、大而圆的眼睛，以及比例很小的
/// 背鳍、臀鳍、胸鳍和尾鳍。鳍的摆动频率明显高于普通鱼，配合缓慢悬停的姿态。
/// </summary>
internal sealed class PufferShape : FishShape
{
    public override double HaloRadiusX => 62.0;

    public override double HaloRadiusY => 50.0;

    private static readonly Geometry Body = Build(ctx =>
    {
        ctx.BeginFigure(new Point(29, 0), true, true);
        ctx.BezierTo(new Point(28, -12), new Point(18, -24), new Point(2, -27), true, true);
        ctx.BezierTo(new Point(-12, -29), new Point(-23, -21), new Point(-26, -7), true, true);

        // 尾柄：身体后缘收窄成一个小尖，尾鳍才接得自然。
        ctx.QuadraticBezierTo(new Point(-30.5, -2), new Point(-30.5, 0), true, true);
        ctx.QuadraticBezierTo(new Point(-30.5, 2), new Point(-26, 7), true, true);

        ctx.BezierTo(new Point(-28, 5), new Point(-21, 19), new Point(-7, 24), true, true);
        ctx.BezierTo(new Point(8, 28), new Point(22, 19), new Point(27, 9), true, true);
        ctx.QuadraticBezierTo(new Point(29, 4), new Point(29, 0), true, true);
    });

    private static readonly Geometry TailFin = Build(ctx =>
    {
        ctx.BeginFigure(new Point(-28, -3), true, true);
        ctx.BezierTo(new Point(-35, -10), new Point(-43, -8), new Point(-42, 0), true, true);
        ctx.BezierTo(new Point(-43, 8), new Point(-35, 10), new Point(-28, 3), true, true);
    });

    private static readonly Geometry DorsalFin = Build(ctx =>
    {
        ctx.BeginFigure(new Point(2, -26.5), true, true);
        ctx.BezierTo(new Point(-2, -33), new Point(-9, -34), new Point(-13, -30), true, true);
        ctx.QuadraticBezierTo(new Point(-6, -27), new Point(2, -26.5), true, true);
    });

    private static readonly Geometry AnalFin = Build(ctx =>
    {
        ctx.BeginFigure(new Point(-4, 24.5), true, true);
        ctx.BezierTo(new Point(-8, 30), new Point(-14, 30.5), new Point(-17, 27), true, true);
        ctx.QuadraticBezierTo(new Point(-10, 24.5), new Point(-4, 24.5), true, true);
    });

    private static readonly Geometry PectoralFin = Build(ctx =>
    {
        ctx.BeginFigure(new Point(10, 6), true, true);
        ctx.BezierTo(new Point(8, 14), new Point(2, 18), new Point(-3, 15), true, true);
        ctx.BezierTo(new Point(1, 10), new Point(6, 7), new Point(10, 6), true, true);
    });

    private static readonly Geometry Spines = BuildSpines();

    /// <summary>吻端的小嘴。</summary>
    private static readonly Geometry Mouth = Build(ctx =>
    {
        ctx.BeginFigure(new Point(27.5, 2.6), false, false);
        ctx.QuadraticBezierTo(new Point(25.5, 4.2), new Point(23.0, 3.4), true, false);
    });

    /// <summary>横贯体侧的荧光带。</summary>
    private static readonly Geometry GlowStripe = Build(ctx =>
    {
        ctx.BeginFigure(new Point(23, -5), true, true);
        ctx.BezierTo(new Point(8, -6), new Point(-12, -4), new Point(-26, -0.5), true, true);
        ctx.BezierTo(new Point(-12, 2.5), new Point(8, 3.5), new Point(23, 2), true, true);
    });

    private static readonly Geometry GlowStripeCore = Build(ctx =>
    {
        ctx.BeginFigure(new Point(23, -2.4), true, true);
        ctx.BezierTo(new Point(8, -3), new Point(-12, -2), new Point(-26, 0), true, true);
        ctx.BezierTo(new Point(-12, 1.2), new Point(8, 1.6), new Point(23, 1.0), true, true);
    });

    private static readonly Geometry GlowCenter = Build(ctx =>
    {
        ctx.BeginFigure(new Point(24, -1.2), false, false);
        ctx.BezierTo(new Point(8, -2.2), new Point(-8, -1.2), new Point(-27, 0.6), true, false);
    });

    public override void Draw(DrawingContext dc, FishPalette palette, double phase, double speed, double sheenPhase)
    {
        double tailBeat = Math.Sin(phase * 1.8) * 12.0;
        double pectoralBeat = Math.Sin(phase * 2.2 + 0.5) * 18.0;
        double finFlutter = Math.Sin(phase * 2.6 + 1.2) * 10.0;

        PaintSwayFin(dc, palette, TailFin, palette.FinLinePen, tailBeat, -28, 0);
        PaintSwayFin(dc, palette, DorsalFin, palette.FinLinePen, finFlutter, -2, -27);
        PaintSwayFin(dc, palette, AnalFin, palette.FinLinePen, -finFlutter, -6, 24);

        PaintBody(dc, palette, Body);

        dc.PushClip(Body);

        // 背深腹浅：圆球身体靠一层上暗下亮的渐变读出体积。
        dc.DrawEllipse(palette.FluoroOuterBrush, null, new Point(-2, -14), 20.0, 9.0);

        // 体表的深色斑纹。
        dc.DrawEllipse(palette.PatchBrush, null, new Point(-9, -6), 6.0, 5.0);
        dc.DrawEllipse(palette.PatchBrush, null, new Point(4, 10), 4.5, 4.0);
        dc.DrawEllipse(palette.PatchBrush, null, new Point(-16, 10), 3.5, 3.0);

        // 体侧荧光带：外层散光、中层主色、中心亮线。
        double pulse = 0.86 + 0.14 * Math.Sin(phase * 0.6);
        dc.PushOpacity(pulse);
        dc.DrawGeometry(palette.FluoroOuterBrush, null, GlowStripe);
        dc.DrawGeometry(palette.FluoroMidBrush, null, GlowStripeCore);
        dc.DrawGeometry(null, palette.GlowCorePen, GlowCenter);
        dc.Pop();

        DrawBodySheen(dc, sheenPhase, -34, 70, -32, 64, 0);

        dc.Pop();

        // 短刺：沿体表一圈，刺尖朝外。
        dc.DrawGeometry(palette.FinFill, palette.RimPen, Spines);

        PaintSwayFin(dc, palette, PectoralFin, palette.FinLinePen, pectoralBeat, 10, 7);

        // 河豚的眼睛很大，几乎占满头侧。
        dc.DrawEllipse(palette.EyeGlowBrush, null, new Point(13.5, -10), 6.0, 6.0);
        DrawEye(dc, palette, new Point(13.5, -10), 5.6, 4.0, 2.1);
        dc.DrawEllipse(
            palette.EyeRingFill,
            null,
            new Point(14.4, -11.2),
            1.0,
            1.0);

        dc.DrawGeometry(null, palette.SeamPen, Mouth);
    }

    /// <summary>沿椭圆轮廓生成一圈向外的小刺，底边贴在体表上。</summary>
    private static Geometry BuildSpines()
    {
        return Build(ctx =>
        {
            const int spikeCount = 18;

            for (int i = 0; i < spikeCount; i++)
            {
                double angle = i / (double)spikeCount * Math.PI * 2;

                // 刺的底边压在体表内侧一点，尖端探出轮廓，才不会像浮在身体外面。
                double px = -1 + Math.Cos(angle) * 24.5;
                double py = Math.Sin(angle) * 22.5;

                double nx = Math.Cos(angle);
                double ny = Math.Sin(angle);
                double tx = -Math.Sin(angle) * 1.6;
                double ty = Math.Cos(angle) * 1.6;

                ctx.BeginFigure(new Point(px - tx, py - ty), true, true);
                ctx.LineTo(new Point(px + nx * 5.2, py + ny * 5.2), true, true);
                ctx.LineTo(new Point(px + tx, py + ty), true, true);
            }
        });
    }
}
