using System.Collections.Generic;
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Skills;

/// <summary>
/// 技能表（首版先硬编码）。
/// <para>
/// 后续可升级为：Godot Resource / JSON / 表格驱动。
/// </para>
/// </summary>
public static class SkillDatabase
{
    /// <summary>
    /// 默认按槽位绑定的技能定义。
    /// </summary>
    public static IReadOnlyDictionary<SkillSlot, SkillDefinition> DefaultBySlot { get; } =
        new Dictionary<SkillSlot, SkillDefinition>
        {
            // Primary：沿用普攻（不在此处实现效果），仍可用于 UI 显示与统一编排。
            [SkillSlot.Primary] = new SkillDefinition(
                SkillId: "skill_primary_melee",
                Slot: SkillSlot.Primary,
                CastType: SkillCastType.Instant,
                EffectKind: SkillEffectKind.None,
                CooldownSeconds: 0f,
                ManaCost: 0f,
                Range: 18f,
                AoeRadius: 0f
            ),
            // Secondary：地面选点 AOE
            [SkillSlot.Secondary] = new SkillDefinition(
                SkillId: "skill_secondary_aoe",
                Slot: SkillSlot.Secondary,
                CastType: SkillCastType.AoeTargeting,
                EffectKind: SkillEffectKind.AoeStrike,
                CooldownSeconds: 4f,
                ManaCost: 25f,
                Range: 120f,
                AoeRadius: 26f
            ),
            [SkillSlot.Skill1] = new SkillDefinition(
                SkillId: "skill1_projectile",
                Slot: SkillSlot.Skill1,
                CastType: SkillCastType.Instant,
                EffectKind: SkillEffectKind.Projectile,
                CooldownSeconds: 1.5f,
                ManaCost: 10f,
                Range: 160f,
                AoeRadius: 0f
            ),
            [SkillSlot.Skill2] = new SkillDefinition(
                SkillId: "skill2_projectile",
                Slot: SkillSlot.Skill2,
                CastType: SkillCastType.Instant,
                EffectKind: SkillEffectKind.Projectile,
                CooldownSeconds: 2.5f,
                ManaCost: 18f,
                Range: 200f,
                AoeRadius: 0f
            ),
            [SkillSlot.Skill3] = new SkillDefinition(
                SkillId: "skill3_aoe_small",
                Slot: SkillSlot.Skill3,
                CastType: SkillCastType.Instant,
                EffectKind: SkillEffectKind.AoeStrike,
                CooldownSeconds: 3f,
                ManaCost: 15f,
                Range: 80f,
                AoeRadius: 16f
            ),
            [SkillSlot.Skill4] = new SkillDefinition(
                SkillId: "skill4_aoe_big",
                Slot: SkillSlot.Skill4,
                CastType: SkillCastType.Instant,
                EffectKind: SkillEffectKind.AoeStrike,
                CooldownSeconds: 6f,
                ManaCost: 35f,
                Range: 90f,
                AoeRadius: 34f
            ),
        };
}
