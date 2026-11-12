using System.Windows;
using System.Windows.Media;
using DesktopFish.Pond;
using DesktopFish.Rendering;

namespace DesktopFish.Controls;

/// <summary>
/// 承载整片鱼塘的绘制表面。不持有自己的动画计时器，
/// 由 <see cref="DesktopFish.MainWindow"/> 驱动引擎并调用 InvalidateVisual。
/// </summary>
public sealed class PondCanvas : FrameworkElement
{
    public PondEngine Engine { get; } = new();

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var fish = Engine.Fish;
        for (int i = 0; i < fish.Count; i++)
        {
            MechFishRenderer.Draw(drawingContext, fish[i]);
        }
    }
}
