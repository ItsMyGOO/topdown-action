using System;
using System.Collections.Generic;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 词条池与抽取规则（纯逻辑）：
/// <para>
/// - 稀有度决定词条数量预算：<c>Common 0 / Magic 1 / Rare 2</c>
/// - 词条属性不重复；数值在属性区间内取整数
/// </para>
/// </summary>
public static class AffixTable
{
    private sealed record AffixDefinition(AffixStat Stat, int MinValue, int MaxValue);

    private static readonly AffixDefinition[] Pool =
    [
        new(AffixStat.BonusMaxMana, 5, 15),
        new(AffixStat.BonusArmor, 5, 15),
        new(AffixStat.BonusXpPercent, 5, 20),
    ];

    /// <summary>
    /// 按稀有度抽取词条行。
    /// </summary>
    public static AffixLine[] Roll(ItemRarity rarity, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var budget = rarity switch
        {
            ItemRarity.Rare => 2,
            ItemRarity.Magic => 1,
            _ => 0,
        };

        if (budget == 0)
        {
            return [];
        }

        var remaining = new List<AffixDefinition>(Pool);
        var lines = new AffixLine[budget];

        for (var i = 0; i < budget; i++)
        {
            var definition = remaining[random.Next(remaining.Count)];
            remaining.Remove(definition);

            var value = (float)random.Next(definition.MinValue, definition.MaxValue + 1);
            lines[i] = new AffixLine(definition.Stat, value);
        }

        return lines;
    }
}
