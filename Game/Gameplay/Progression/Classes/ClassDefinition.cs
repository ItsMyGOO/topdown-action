using System.Collections.Generic;
using System.Linq;

namespace GodotGameTemplate.Gameplay.Progression.Classes;

/// <summary>
/// 职业定义（纯数据）。
/// </summary>
/// <param name="Id">稳定标识（用于存档）。</param>
/// <param name="Name">展示名。</param>
/// <param name="Description">倾向描述。</param>
/// <param name="DamageMultiplier">攻击伤害倍率。</param>
/// <param name="HealthMultiplier">最大生命倍率。</param>
/// <param name="ManaMultiplier">最大法力倍率。</param>
/// <param name="MoveSpeedMultiplier">移动速度倍率。</param>
public sealed record ClassDefinition(
    string Id,
    string Name,
    string Description,
    float DamageMultiplier,
    float HealthMultiplier,
    float ManaMultiplier,
    float MoveSpeedMultiplier
);

/// <summary>
/// 职业目录（D4 风格三职业雏形）。
/// </summary>
public static class ClassDatabase
{
    public const string Barbarian = "barbarian";
    public const string Sorcerer = "sorcerer";
    public const string Rogue = "rogue";

    public const string DefaultClassId = Barbarian;

    public static IReadOnlyList<ClassDefinition> Catalog { get; } =
    [
        new ClassDefinition(
            Barbarian,
            "野蛮人",
            "近战厚血高伤",
            DamageMultiplier: 1.3f,
            HealthMultiplier: 1.25f,
            ManaMultiplier: 0.6f,
            MoveSpeedMultiplier: 1.0f
        ),
        new ClassDefinition(
            Sorcerer,
            "法师",
            "远程法术脆皮",
            DamageMultiplier: 0.9f,
            HealthMultiplier: 0.8f,
            ManaMultiplier: 1.5f,
            MoveSpeedMultiplier: 1.0f
        ),
        new ClassDefinition(
            Rogue,
            "游侠",
            "敏捷均衡机动",
            DamageMultiplier: 1.1f,
            HealthMultiplier: 1.0f,
            ManaMultiplier: 0.9f,
            MoveSpeedMultiplier: 1.2f
        ),
    ];

    /// <summary>
    /// 读取职业定义；未知 Id 回退默认职业。
    /// </summary>
    public static ClassDefinition Get(string id)
    {
        return Catalog.FirstOrDefault(definition => definition.Id == id) ?? Catalog[0];
    }
}
