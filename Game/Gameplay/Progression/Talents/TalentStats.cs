namespace GodotGameTemplate.Gameplay.Progression.Talents;

/// <summary>
/// 天赋属性汇总（纯逻辑）。
/// </summary>
/// <param name="BonusDamage">平加攻击伤害。</param>
/// <param name="BonusMaxHealth">平加最大生命。</param>
/// <param name="BonusMaxMana">平加最大法力。</param>
/// <param name="EvadeRechargeSecondsReduction">闪避充能回复缩短秒数。</param>
/// <param name="XpMultiplier">经验倍率。</param>
/// <param name="MoveSpeedMultiplier">移动速度倍率。</param>
public readonly record struct TalentStatSummary(
    int BonusDamage,
    float BonusMaxHealth,
    float BonusMaxMana,
    float EvadeRechargeSecondsReduction,
    float XpMultiplier,
    float MoveSpeedMultiplier
);

/// <summary>
/// 天赋属性聚合器：把天赋等级折叠成最终属性加成。
/// </summary>
public static class TalentStats
{
    public static TalentStatSummary Aggregate(TalentModel talents)
    {
        var bonusDamage = 0;
        float bonusHealth = 0f;
        float bonusMana = 0f;
        var evadeRechargeReduction = 0f;
        var xpPercent = 0f;
        var speedPercent = 0f;

        foreach (var (id, rank) in talents.Ranks)
        {
            switch (id)
            {
                case TalentDatabase.Might:
                    bonusDamage += rank;
                    break;
                case TalentDatabase.Toughness:
                    bonusHealth += 10f * rank;
                    break;
                case TalentDatabase.Meditation:
                    bonusMana += 5f * rank;
                    break;
                case TalentDatabase.Fleetfooted:
                    evadeRechargeReduction += 1f * rank;
                    break;
                case TalentDatabase.Wisdom:
                    xpPercent += 5f * rank;
                    break;
                case TalentDatabase.Swiftness:
                    speedPercent += 8f * rank;
                    break;
            }
        }

        return new TalentStatSummary(
            bonusDamage,
            bonusHealth,
            bonusMana,
            evadeRechargeReduction,
            1f + xpPercent / 100f,
            1f + speedPercent / 100f
        );
    }
}
