namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 词缀属性类型（首版只做现有系统可感知的三类）。
/// </summary>
public enum AffixStat
{
    BonusMaxMana,

    BonusMaxStamina,

    BonusXpPercent,
}

/// <summary>
/// 一条随机词缀（属性 + 数值）。数值语义由 <see cref="Stat"/> 决定：
/// 平加属性用绝对值，百分比属性（如 <see cref="AffixStat.BonusXpPercent"/>）用百分数。
/// </summary>
public readonly record struct AffixLine(AffixStat Stat, float Value);
