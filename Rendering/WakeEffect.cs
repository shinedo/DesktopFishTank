using System;
using System.Windows;
using System.Windows.Media;
using DesktopFishTank.Pond;

namespace DesktopFishTank.Rendering;

/// <summary>
/// 游动时偶尔从尾部浮起的小气泡：一圈很淡的亮边加一点高光，
/// 随年龄变淡。全部使用预生成的分级画笔，不逐气泡开透明度分组。
/// </summary>
internal static class WakeEffect
{
    public static void DrawBubbles(DrawingContext dc, MechFish fish, FishPalette palette)
    {
        for (int i = 0; i < fish.Wake.Length; i++)
        {
            ref WakeParticle particle = ref fish.Wake[i];
            if (!particle.Alive)
            {
                continue;
            }

            double life = particle.Age / particle.Lifetime;
            if (life >= 1.0)
            {
                continue;
            }

            int level = Math.Clamp((int)((1.0 - life) * palette.BubblePens.Length), 0, palette.BubblePens.Length - 1);
            double radius = particle.Scale;

            dc.DrawEllipse(null, palette.BubblePens[level], particle.Position, radius, radius);
            dc.DrawEllipse(
                palette.BubbleHighlight,
                null,
                new Point(particle.Position.X - radius * 0.3, particle.Position.Y - radius * 0.32),
                radius * 0.28,
                radius * 0.28);
        }
    }
}
