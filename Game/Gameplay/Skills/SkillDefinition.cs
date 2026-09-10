using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Skills;

/// <summary>
/// 技能释放类型：
/// <para>
/// - <see cref="Instant"/>：按下即释放
/// - <see cref="AoeTargeting"/>：进入地面选点/指示器模式后确认释放
/// </para>
/// </summary>
public enum SkillCastType
{
    Instant = 0,
    AoeTargeting = 1,
}

/// <summary>
/// 技能效果类型（首版最小集合）。
/// </summary>
public enum SkillEffectKind
{
    None = 0,
    Projectile = 1,
    AoeStrike = 2,
}

/// <summary>
/// 技能定义（纯数据）。
/// </summary>
/// <param name="SkillId">技能标识。</param>
/// <param name="Slot">技能槽位（6 槽）。</param>
/// <param name="CastType">释放类型。</param>
/// <param name="EffectKind">效果类型。</param>
/// <param name="CooldownSeconds">冷却时间（秒）。</param>
/// <param name="ManaCost">法力消耗。</param>
/// <param name="Range">施法距离（像素）。</param>
/// <param name="AoeRadius">范围半径（像素）。</param>
public sealed record SkillDefinition(
    string SkillId,
    SkillSlot Slot,
    SkillCastType CastType,
    SkillEffectKind EffectKind,
    float CooldownSeconds,
    float ManaCost,
    float Range,
    float AoeRadius
);
