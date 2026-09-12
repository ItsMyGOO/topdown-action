using System.Collections.Generic;
using System.Linq;
using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Gameplay.Progression.Tiers;

/// <summary>
/// 世界等级定义（纯数据）。
/// </summary>
/// <param name="Tier">等级编号（1 起）。</param>
/// <param name="Name">展示名。</param>
/// <param name="EnemyHpMultiplier">敌人生命倍率。</param>
/// <param name="EnemyDamageMultiplier">敌人伤害倍率。</param>
/// <param name="XpMultiplier">经验倍率。</param>
/// <param name="DropRarityFloor">掉落稀有度下限。</param>
public sealed record WorldTierDefinition(
    int Tier,
    string Name,
    float EnemyHpMultiplier,
    float EnemyDamageMultiplier,
    float XpMultiplier,
    ItemRarity DropRarityFloor
);

/// <summary>
/// 世界等级目录（D4 难度分层雏形：普通/噩梦/地狱）。
/// </summary>
public static class WorldTierDatabase
{
    public static IReadOnlyList<WorldTierDefinition> Catalog { get; } =
    [
        new WorldTierDefinition(
            1,
            "普通",
            EnemyHpMultiplier: 1f,
            EnemyDamageMultiplier: 1f,
            XpMultiplier: 1f,
            DropRarityFloor: ItemRarity.Common
        ),
        new WorldTierDefinition(
            2,
            "噩梦",
            EnemyHpMultiplier: 2.5f,
            EnemyDamageMultiplier: 1.8f,
            XpMultiplier: 1.75f,
            DropRarityFloor: ItemRarity.Rare
        ),
        new WorldTierDefinition(
            3,
            "地狱",
            EnemyHpMultiplier: 5f,
            EnemyDamageMultiplier: 2.6f,
            XpMultiplier: 2.5f,
            DropRarityFloor: ItemRarity.Rare
        ),
    ];

    /// <summary>
    /// 读取世界等级；越界值钳制到有效区间。
    /// </summary>
    public static WorldTierDefinition Get(int tier)
    {
        var clamped = System.Math.Clamp(tier, 1, Catalog.Count);

        return Catalog.First(definition => definition.Tier == clamped);
    }
}
