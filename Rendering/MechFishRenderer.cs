using System;
using System.Windows;
using System.Windows.Media;
using DesktopFish.Pond;

namespace DesktopFish.Rendering;

/// <summary>
/// 把一条 <see cref="MechFish"/> 画到 DrawingContext。
/// 统一处理透明度、位置、朝向、镜像与缩放，造型固定为荧光玻璃鱼。
/// 所有发光都用多层半透明图形模拟，不使用 Effect，
/// 避免透明窗口在软件渲染路径下产生额外开销。
/// </summary>
internal static class MechFishRenderer
{
    private static readonly FishShape Shape = new GlassFishShape();

    public static void Draw(DrawingContext dc, MechFish fish)
    {
        FishPalette palette = fish.Palette;
        double scale = fish.RenderScale;

        if (scale <= 0.01)
        {
            return;
        }

        double depthRatio = (fish.Depth - 0.66) / 0.34;

        // 远处至少保留 62% 不透明度：形体靠亮边和发光带支撑，压太暗就看不见了。
        double depthOpacity = Math.Clamp(0.62 + 0.38 * depthRatio, 0.62, 1.0);
        double fishOpacity = depthOpacity * fish.Opacity;

        // 朝左游时做垂直镜像，避免鱼整体翻转成肚皮朝上。
        bool mirror = Math.Cos(fish.Heading) < 0;

        dc.PushTransform(new TranslateTransform(fish.Position.X, fish.Position.Y));
        dc.PushTransform(new RotateTransform(fish.Heading * 180.0 / Math.PI));

        int transformDepth = 2;
        if (mirror)
        {
            dc.PushTransform(new ScaleTransform(1, -1));
            transformDepth++;
        }

        dc.PushTransform(new ScaleTransform(scale, scale));
        transformDepth++;

        // 水下光晕：宽而低的径向渐变，让鱼在深色桌面上有“发光体”感。
        // 它不跟随“鱼的透明度”等比变淡——否则调到 40% 时整圈光晕会直接消失；
        // 这里只保留一部分衰减，让光晕始终看得见，又不会盖过变透的鱼身。
        if (fish.ShowHalo)
        {
            dc.PushOpacity(depthOpacity * (0.45 + 0.55 * fish.Opacity));
            dc.DrawEllipse(
                palette.HaloBrush,
                null,
                new Point(2, 0),
                Shape.HaloRadiusX,
                Shape.HaloRadiusY);
            dc.Pop();
        }

        dc.PushOpacity(fishOpacity);
        Shape.Draw(dc, palette, fish.Phase, fish.Speed);
        dc.Pop();

        for (int i = 0; i < transformDepth; i++)
        {
            dc.Pop();
        }
    }
}
