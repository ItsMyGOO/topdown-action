namespace GodotGameTemplate.Gameplay.Progression.Paragon;

/// <summary>
/// Paragon 属性汇总（纯逻辑）。
/// </summary>
/// <param name="BonusDamage">平加攻击伤害。</param>
/// <param name="BonusMaxHealth">平加最大生命。</param>
/// <param name="XpMultiplier">经验倍率。</param>
/// <param name="MoveSpeedMultiplier">移速倍率。</param>
public readonly record struct ParagonStatSummary(
    int BonusDamage,
    float BonusMaxHealth,
    float XpMultiplier,
    float MoveSpeedMultiplier
);

/// <summary>
/// Paragon 属性聚合器（简化版线性加成）。
/// </summary>
public static class ParagonStats
{
    public static ParagonStatSummary Aggregate(ParagonModel paragon)
    {
        var bonusDamage = 0;
        float bonusHealth = 0f;
        float xpPercent = 0f;
        float speedPercent = 0f;

        foreach (var (category, rank) in paragon.Ranks)
        {
            switch (category)
            {
                case ParagonCategory.Brutality:
                    bonusDamage += 1 * rank;
                    break;
                case ParagonCategory.Vitality:
                    bonusHealth += 3f * rank;
                    break;
                case ParagonCategory.Cunning:
                    xpPercent += 1f * rank;
                    break;
                case ParagonCategory.Alacrity:
                    speedPercent += 0.5f * rank;
                    break;
            }
        }

        return new ParagonStatSummary(
            bonusDamage,
            bonusHealth,
            1f + xpPercent / 100f,
            1f + speedPercent / 100f
        );
    }
}
