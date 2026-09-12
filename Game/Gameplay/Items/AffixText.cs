using System;
using System.Collections.Generic;
using System.Linq;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 词缀展示文本（纯逻辑，供 UI 使用）。
/// </summary>
public static class AffixText
{
    public static string Format(AffixLine affix)
    {
        return affix.Stat switch
        {
            AffixStat.BonusMaxMana => $"+{affix.Value:0} 最大法力",
            AffixStat.BonusArmor => $"+{affix.Value:0} 护甲",
            AffixStat.BonusXpPercent => $"+{affix.Value:0}% 经验获取",
            _ => throw new ArgumentOutOfRangeException(nameof(affix)),
        };
    }

    public static string FormatAll(IEnumerable<AffixLine> affixes)
    {
        return string.Join("，", affixes.Select(Format));
    }
}
