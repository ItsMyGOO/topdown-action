using System.Collections.Generic;
using System.Linq;

namespace GodotGameTemplate.Gameplay.Progression.Talents;

/// <summary>
/// 一条天赋定义（纯数据）。
/// </summary>
/// <param name="Id">稳定标识（用于存档）。</param>
/// <param name="Name">展示名。</param>
/// <param name="Description">每级效果说明。</param>
/// <param name="MaxRank">最大可加等级。</param>
/// <param name="RequiresId">前置天赋 Id（null 表示无前置）。</param>
/// <param name="RequiresRank">前置天赋所需等级。</param>
public sealed record TalentDefinition(
    string Id,
    string Name,
    string Description,
    int MaxRank,
    string? RequiresId = null,
    int RequiresRank = 0
);

/// <summary>
/// 天赋目录（静态，首版 6 条被动天赋）。
/// </summary>
public static class TalentDatabase
{
    public const string Might = "might";
    public const string Toughness = "toughness";
    public const string Meditation = "meditation";
    public const string Fleetfooted = "fleetfooted";
    public const string Wisdom = "wisdom";
    public const string Swiftness = "swiftness";

    public static IReadOnlyList<TalentDefinition> Catalog { get; } =
    [
        new TalentDefinition(Might, "力量", "+1 攻击伤害", 3),
        new TalentDefinition(
            Toughness,
            "坚韧",
            "+10 最大生命",
            3,
            RequiresId: Might,
            RequiresRank: 1
        ),
        new TalentDefinition(Meditation, "冥想", "+5 最大法力", 3),
        new TalentDefinition(Fleetfooted, "轻足", "闪避充能回复 -1 秒", 2),
        new TalentDefinition(
            Wisdom,
            "智慧",
            "+5% 经验获取",
            2,
            RequiresId: Meditation,
            RequiresRank: 1
        ),
        new TalentDefinition(Swiftness, "疾行", "+8% 移动速度", 2),
    ];

    public static TalentDefinition? Get(string id)
    {
        return Catalog.FirstOrDefault(talent => talent.Id == id);
    }
}
