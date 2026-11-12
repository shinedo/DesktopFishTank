using System.Collections.Generic;
using System.Windows.Media;

namespace DesktopFishTank.Rendering;

/// <summary>
/// 荧光玻璃鱼的配色库。强调色决定体侧荧光带，副强调色决定体后暖斑与内脏色，
/// 体壁本身几乎不着色，所以换色就是换“发什么光”。
/// 每套配色都有名字，鱼群设置里可以逐项开关。
/// </summary>
public static class FishPaletteCatalog
{
    public static IReadOnlyList<FishPalette> All { get; } = new[]
    {
        FishPalette.Create("青绿", C(0x3A, 0xFF, 0xD2), C(0xFF, 0x4D, 0x5E)),
        FishPalette.Create("冰蓝", C(0x5A, 0xD8, 0xFF), C(0xFF, 0x8A, 0x3D)),
        FishPalette.Create("紫青", C(0x9B, 0x8C, 0xFF), C(0xFF, 0x4F, 0xD8)),
        FishPalette.Create("荧光黄绿", C(0xB6, 0xFF, 0x57), C(0x38, 0xC8, 0xFF)),
        FishPalette.Create("玫红", C(0xFF, 0x4F, 0x7A), C(0x6C, 0xFF, 0xD8)),
        FishPalette.Create("琥珀", C(0xFF, 0xB3, 0x47), C(0x3A, 0xE0, 0xFF)),
        FishPalette.Create("极光", C(0x3D, 0xFF, 0xB0), C(0x7B, 0x5C, 0xFF)),
        FishPalette.Create("深海", C(0x3D, 0x9B, 0xFF), C(0xB6, 0xFF, 0x57)),
        FishPalette.Create("樱粉", C(0xFF, 0x7A, 0xD9), C(0x5A, 0xE0, 0xFF)),
        FishPalette.Create("青柠", C(0xC8, 0xFF, 0x3D), C(0xFF, 0x3D, 0x8A)),
        FishPalette.Create("紫罗兰", C(0xC7, 0x7B, 0xFF), C(0x4D, 0xFF, 0xC4)),
        FishPalette.Create("白青", C(0x9F, 0xE8, 0xFF), C(0xFF, 0xC2, 0x4D)),
        FishPalette.Create("珊瑚", C(0xFF, 0x6B, 0x35), C(0x3A, 0xE0, 0xFF)),
        FishPalette.Create("薄荷", C(0x5C, 0xFF, 0xC0), C(0xFF, 0x6B, 0x9D)),
        FishPalette.Create("电光紫", C(0xB4, 0x4D, 0xFF), C(0x3D, 0xFF, 0xB0)),
        FishPalette.Create("冰晶", C(0xD8, 0xF6, 0xFF), C(0xFF, 0x7A, 0x3D)),
        FishPalette.Create("深海青", C(0x00, 0xE5, 0xC0), C(0xFF, 0x4D, 0x6D)),
        FishPalette.Create("桃粉", C(0xFF, 0x9E, 0xB5), C(0x6C, 0xFF, 0xD8)),
        FishPalette.Create("柠檬", C(0xF5, 0xFF, 0x4D), C(0x5A, 0x9B, 0xFF)),
        FishPalette.Create("天青", C(0x6C, 0xD8, 0xFF), C(0xC7, 0x7B, 0xFF)),
    };

    public static FishPalette Default => All[0];

    /// <summary>按显示名查下标，找不到返回 -1。</summary>
    public static int IndexOfName(string name)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>全部配色的下标，作为“一种都没排除”的默认值。</summary>
    public static List<int> AllIndices()
    {
        var indices = new List<int>(All.Count);
        for (int i = 0; i < All.Count; i++)
        {
            indices.Add(i);
        }

        return indices;
    }

    private static Color C(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
}
