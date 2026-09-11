using System;
using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Gameplay.Enemies;

/// <summary>
/// 精英词缀（首版：每精英一个词缀，效果均为当前系统可感知的数值强化）。
/// </summary>
public enum EliteAffix
{
    None,

    /// <summary>强壮：生命 ×3、经验 ×2。</summary>
    Sturdy,

    /// <summary>狡诈：经验 ×2、掉落 2 件且稀有度不低于魔法。</summary>
    Cunning,

    /// <summary>富有：击杀立即掉落金币。</summary>
    Rich,

    /// <summary>迅捷：移动速度 ×1.6、经验 ×1.25。</summary>
    Swift,
}

/// <summary>
/// 一次战斗强化计划：数值在生成期全部定死，胶水层只负责执行。
/// </summary>
/// <param name="IsBoss">是否为 Boss。</param>
/// <param name="Affix">精英词缀（Boss 为 None）。</param>
/// <param name="HpMultiplier">生命值倍率。</param>
/// <param name="XpMultiplier">经验倍率。</param>
/// <param name="GoldBonus">击杀立即掉落的金币（0 表示无）。</param>
/// <param name="DropCount">死亡掉落件数。</param>
/// <param name="RarityFloor">掉落稀有度下限。</param>
/// <param name="DamageMultiplier">敌人伤害倍率。</param>
/// <param name="SpeedMultiplier">敌人移动速度倍率。</param>
public sealed record ElitePlan(
    bool IsBoss,
    EliteAffix Affix,
    float HpMultiplier,
    float XpMultiplier,
    int GoldBonus,
    int DropCount,
    ItemRarity RarityFloor,
    float DamageMultiplier = 1f,
    float SpeedMultiplier = 1f
);

/// <summary>
/// 精英计划掷取（纯逻辑，可注入种子做确定性测试）。
/// </summary>
public static class EliteRoller
{
    /// <summary>
    /// 普通怪被升为精英的默认概率。
    /// </summary>
    public const double EliteChance = 0.15;

    /// <summary>
    /// 以给定概率掷一次精英化；未命中返回 <c>null</c>。
    /// 命中时等概率抽一个词缀并生成计划（金币数在此期掷定）。
    /// </summary>
    public static ElitePlan? TryRoll(double chance, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (random.NextDouble() >= chance)
        {
            return null;
        }

        var affix = (EliteAffix)random.Next(1, 5);
        return CreatePlan(false, affix, random);
    }

    /// <summary>
    /// Boss 固定强化计划。
    /// </summary>
    public static ElitePlan Boss()
    {
        return new ElitePlan(true, EliteAffix.None, 10f, 10f, 50, 2, ItemRarity.Rare, 3f, 1.15f);
    }

    private static ElitePlan CreatePlan(bool isBoss, EliteAffix affix, Random random)
    {
        return affix switch
        {
            EliteAffix.Sturdy => new ElitePlan(isBoss, affix, 3f, 2f, 0, 1, ItemRarity.Common, 2f),
            EliteAffix.Cunning => new ElitePlan(isBoss, affix, 1f, 2f, 0, 2, ItemRarity.Magic, 1f),
            EliteAffix.Rich => new ElitePlan(
                isBoss,
                affix,
                1f,
                1f,
                random.Next(10, 26),
                1,
                ItemRarity.Common,
                1f
            ),
            EliteAffix.Swift => new ElitePlan(
                isBoss,
                affix,
                1f,
                1.25f,
                0,
                1,
                ItemRarity.Common,
                1f,
                1.6f
            ),
            _ => new ElitePlan(isBoss, affix, 1f, 1f, 0, 1, ItemRarity.Common, 1f),
        };
    }
}
