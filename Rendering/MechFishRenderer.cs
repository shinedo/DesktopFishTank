using System;
using System.Windows;
using System.Windows.Media;
using DesktopFishTank.Pond;

namespace DesktopFishTank.Rendering;

/// <summary>
/// 把一条 <see cref="MechFish"/> 画到 DrawingContext。
/// 统一处理透明度、位置、朝向、镜像与缩放，造型固定为荧光玻璃鱼。
/// 所有发光都用多层半透明图形模拟，不使用 Effect，
/// 避免透明窗口在软件渲染路径下产生额外开销。
/// </summary>
internal static class MechFishRenderer
{
    private static readonly FishShape GlassShape = new GlassFishShape();
    private static readonly FishShape KrillShapeImpl = new KrillShape();
    private static readonly FishShape JellyfishShapeImpl = new JellyfishShape();

    public static void Draw(DrawingContext dc, MechFish fish)
    {
        FishPalette palette = fish.Palette;
        FishShape shape = fish.Species switch
        {
            FishSpecies.Krill => KrillShapeImpl,
            FishSpecies.Jellyfish => JellyfishShapeImpl,
            _ => GlassShape,
        };
        double scale = fish.RenderScale;

        if (scale <= 0.01)
        {
            return;
        }

        // 气泡用世界坐标绘制，先于鱼身，落在身体后面。
        WakeEffect.DrawBubbles(dc, fish, palette);

        // 朝左游时做垂直镜像，避免鱼整体翻转成肚皮朝上。
        // 不再按远近压透明度：WPF 软件渲染下每个 PushOpacity 都要离屏合成一次，
        // 每条鱼两层分组会让绘制开销随鱼数线性增长。远近改由缩放表达。
        bool mirror = Math.Cos(fish.Heading) < 0;

        dc.PushTransform(new TranslateTransform(fish.Position.X, fish.Position.Y));

        int transformDepth = 1;
        if (shape.Upright)
        {
            // 直立体型不按航向旋转，只按上下游方向做很小的倾斜；
            // 左右朝向用水平镜像表达，避免身体整个倒过来。
            dc.PushTransform(new RotateTransform(Math.Sin(fish.Heading) * 5.0));
            transformDepth++;

            if (mirror)
            {
                dc.PushTransform(new ScaleTransform(-1, 1));
                transformDepth++;
            }
        }
        else
        {
            dc.PushTransform(new RotateTransform(fish.Heading * 180.0 / Math.PI));
            transformDepth++;

            // 朝左游时做垂直镜像，避免鱼整体翻转成肚皮朝上。
            if (mirror)
            {
                dc.PushTransform(new ScaleTransform(1, -1));
                transformDepth++;
            }
        }

        dc.PushTransform(new ScaleTransform(scale, scale));
        transformDepth++;

        // 水下光晕：宽而低的径向渐变，让鱼在深色桌面上有“发光体”感。
        if (fish.ShowHalo)
        {
            dc.DrawEllipse(
                palette.HaloBrush,
                null,
                new Point(2, 0),
                shape.HaloRadiusX,
                shape.HaloRadiusY);
        }

        shape.Draw(dc, palette, fish.Phase, fish.Speed, fish.SheenPhase);

        for (int i = 0; i < transformDepth; i++)
        {
            dc.Pop();
        }
    }
}
