using System.Collections.Generic;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 装备属性汇总（纯逻辑）。
/// </summary>
/// <param name="MaxManaBonus">平加最大法力。</param>
/// <param name="MaxStaminaBonus">平加最大体力。</param>
/// <param name="XpMultiplier">经验倍率（1 + 百分比词条之和/100）。</param>
public readonly record struct EquipmentStatSummary(
    float MaxManaBonus,
    float MaxStaminaBonus,
    float XpMultiplier
);

/// <summary>
/// 装备属性聚合器：把全部已装备词条折叠成最终加成。
/// </summary>
public static class EquipmentStats
{
    public static EquipmentStatSummary Summarize(EquipmentModel equipment)
    {
        float maxMana = 0f;
        float maxStamina = 0f;
        var xpPercent = 0f;

        foreach (var item in equipment.EquippedItems)
        {
            foreach (var affix in item.Affixes)
            {
                switch (affix.Stat)
                {
                    case AffixStat.BonusMaxMana:
                        maxMana += affix.Value;
                        break;
                    case AffixStat.BonusMaxStamina:
                        maxStamina += affix.Value;
                        break;
                    case AffixStat.BonusXpPercent:
                        xpPercent += affix.Value;
                        break;
                }
            }
        }

        return new EquipmentStatSummary(maxMana, maxStamina, 1f + xpPercent / 100f);
    }
}
