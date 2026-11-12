using System.Windows;
using System.Windows.Media;
using DesktopFishTank.Pond;
using DesktopFishTank.Rendering;

namespace DesktopFishTank.Controls;

/// <summary>
/// 承载整片鱼塘的绘制表面。不持有自己的动画计时器，
/// 由 <see cref="DesktopFishTank.MainWindow"/> 驱动引擎并调用 InvalidateVisual。
/// </summary>
public sealed class PondCanvas : FrameworkElement
{
    public PondEngine Engine { get; } = new();

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        // 整体透明度只开一层分组：逐鱼 PushOpacity 在软件渲染下会为每条鱼
        // 各开一张离屏表面，鱼一多帧时间就成倍增长。
        double opacity = Engine.FishOpacity;
        bool fade = opacity < 0.999;

        if (fade)
        {
            drawingContext.PushOpacity(opacity);
        }

        var fish = Engine.Fish;
        for (int i = 0; i < fish.Count; i++)
        {
            MechFishRenderer.Draw(drawingContext, fish[i]);
        }

        if (fade)
        {
            drawingContext.Pop();
        }
    }
}
