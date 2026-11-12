using System;
using System.Collections.Generic;

namespace DesktopFishTank.Rendering;

/// <summary>鱼塘里可以出现的生物品种。</summary>
public enum FishSpecies
{
    GlassFish = 0,
    Krill = 1,
    Jellyfish = 2,
}

/// <summary>一个品种的显示名与随机出现权重。</summary>
public sealed record FishSpeciesInfo(FishSpecies Species, string Name, int Weight);

/// <summary>
/// 品种库。设置窗口按 <see cref="Name"/> 逐项开关，配置文件也按名字持久化，
/// 以后新增品种不会打乱已有选择。
/// </summary>
public static class FishSpeciesCatalog
{
    public static IReadOnlyList<FishSpeciesInfo> All { get; } = new[]
    {
        new FishSpeciesInfo(FishSpecies.GlassFish, "荧光鱼", 50),
        new FishSpeciesInfo(FishSpecies.Krill, "磷虾", 25),
        new FishSpeciesInfo(FishSpecies.Jellyfish, "水母", 15),
    };

    /// <summary>按显示名查品种，找不到返回 null。</summary>
    public static FishSpecies? FindByName(string name)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].Name == name)
            {
                return All[i].Species;
            }
        }

        return null;
    }

    /// <summary>全部品种，作为“一种都没排除”的默认值。</summary>
    public static List<FishSpecies> AllSpecies()
    {
        var species = new List<FishSpecies>(All.Count);
        for (int i = 0; i < All.Count; i++)
        {
            species.Add(All[i].Species);
        }

        return species;
    }

    public static int WeightOf(FishSpecies species)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].Species == species)
            {
                return Math.Max(1, All[i].Weight);
            }
        }

        return 1;
    }
}
