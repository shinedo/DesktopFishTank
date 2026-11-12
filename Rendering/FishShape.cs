using System;
using System.Windows;
using System.Windows.Media;

namespace DesktopFishTank.Rendering;

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

    /// <summary>
    /// true 表示造型始终保持直立，不随游动方向整体旋转，
    /// 左右朝向改由水平镜像表达。
    /// </summary>
    public virtual bool Upright => false;

    /// <summary>扫过体侧的柔光带，所有品种共用，随相位横向移动。</summary>
    protected static readonly Brush BodySheenBrush = CreateBodySheenBrush();

    /// <summary>骨刺：比鳍膜亮一档的白线，桌面尺寸下也读得出“骨”的结构。</summary>
    protected static readonly Pen FinRayPen = CreateFinRayPen();

    /// <summary>扫光占整个周期的比例，剩余部分留作暗场间隔。</summary>
    private const double SheenDuty = 0.80;

    public abstract void Draw(DrawingContext dc, FishPalette palette, double phase, double speed, double sheenPhase);

    /// <summary>
    /// 在已经裁到身体轮廓的绘制上下文里，画一道从头部向尾部扫过的折射光带。
    /// 局部坐标头朝 +X，所以扫光从右往左走。各品种只需给出扫过范围和倾斜角。
    /// <paramref name="sheenPhase"/> 在 0-1 间循环，后五分之一为暗场，让两次扫光之间留出停顿。
    /// </summary>
    protected static void DrawBodySheen(
        DrawingContext dc,
        double sheenPhase,
        double left,
        double span,
        double top,
        double height,
        double angle)
    {
        const double width = 15.0;
        double cycle = sheenPhase % 1.0;
        if (cycle >= SheenDuty)
        {
            return;
        }

        double t = cycle / SheenDuty;
        double x = left + span - width - t * span;

        dc.PushTransform(new RotateTransform(angle, 0, 0));
        dc.DrawRectangle(BodySheenBrush, null, new Rect(x, top, width, height));
        dc.Pop();
    }

    private static Pen CreateFinRayPen()
    {
        var brush = new SolidColorBrush(Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF));
        brush.Freeze();

        var pen = new Pen(brush, 0.8);
        pen.Freeze();
        return pen;
    }

    private static Brush CreateBodySheenBrush()
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
