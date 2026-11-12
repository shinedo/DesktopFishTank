using System;
using System.Windows;
using System.Windows.Media;

namespace DesktopFish.Rendering;

/// <summary>
/// 一种鱼的矢量绘制逻辑。位置、朝向、镜像、缩放与透明度由 <see cref="MechFishRenderer"/>
/// 统一处理，子类只负责在自己的局部坐标系里画部件。
/// 鱼头朝 +X，原点位于鱼身中部，单位长度约 100。
/// </summary>
internal abstract class FishShape
{
    /// <summary>水下光晕的横向半径。</summary>
    public virtual double HaloRadiusX => 80.0;

    /// <summary>水下光晕的纵向半径。</summary>
    public virtual double HaloRadiusY => 38.0;

    public abstract void Draw(DrawingContext dc, FishPalette palette, double phase, double speed);

    protected static Geometry Build(Action<StreamGeometryContext> build)
    {
        var geometry = new StreamGeometry();
        using (StreamGeometryContext ctx = geometry.Open())
        {
            build(ctx);
        }

        geometry.Freeze();
        return geometry;
    }

    /// <summary>躯干：半透明填充 + 上下缘渐亮的玻璃亮边。</summary>
    protected static void PaintBody(DrawingContext dc, FishPalette palette, Geometry geometry)
    {
        dc.DrawGeometry(palette.BodyFill, palette.RimPen, geometry);
    }

    /// <summary>鳍：半透明薄膜 + 各自的边线画笔。</summary>
    protected static void PaintFin(DrawingContext dc, FishPalette palette, Geometry geometry, Pen edge)
    {
        dc.DrawGeometry(palette.FinFill, edge, geometry);
    }

    /// <summary>
    /// 绕枢轴小幅摆动的鳍。幅度小，用来让背鳍、胸鳍、腹鳍跟着游姿轻轻浮动，
    /// 而不是整块刚体一样钉在鱼身上。
    /// </summary>
    protected static void PaintSwayFin(
        DrawingContext dc,
        FishPalette palette,
        Geometry geometry,
        Pen edge,
        double angle,
        double pivotX,
        double pivotY)
    {
        dc.PushTransform(new RotateTransform(angle, pivotX, pivotY));
        PaintFin(dc, palette, geometry, edge);
        dc.Pop();
    }

    /// <summary>眼窝：外发光 → 眼眶 → 亮色瞳孔。</summary>
    protected static void DrawEye(
        DrawingContext dc,
        FishPalette palette,
        Point center,
        double glow,
        double ring,
        double core)
    {
        dc.DrawEllipse(palette.EyeGlowBrush, null, center, glow, glow);
        dc.DrawEllipse(palette.EyeRingFill, palette.EyeRingPen, center, ring, ring);
        dc.DrawEllipse(palette.EyeCoreBrush, null, center, core, core);
    }
}
