using System.Collections.Generic;
using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Progression;

namespace GodotGameTemplate.Gameplay.Skills;

/// <summary>
/// 技能编排器（纯逻辑）：把“技能按键触发”转换为“进入施法/瞄准状态”的决策。
/// </summary>
public sealed class SkillOrchestrator
{
    public SkillDecision Evaluate(
        PlayerCommand command,
        IReadOnlyDictionary<SkillSlot, SkillDefinition> skills,
        ManaModel mana,
        CooldownModel cooldowns,
        bool isBusy
    )
    {
        if (isBusy)
        {
            return command.AnySkillPressed
                ? SkillDecision.Reject(SkillRejectReason.Busy, SkillSlot.Primary)
                : SkillDecision.None;
        }

        if (!command.AnySkillPressed)
        {
            return SkillDecision.None;
        }

        // 优先级：Secondary > Primary > Skill1..4（避免同帧多按时“误施放”）。
        var slot = ResolvePressedSlot(command);
        if (!skills.TryGetValue(slot, out var def))
        {
            return SkillDecision.None;
        }

        if (!cooldowns.IsReady(slot))
        {
            return SkillDecision.Reject(SkillRejectReason.Cooldown, slot);
        }

        if (mana.Current < def.ManaCost)
        {
            return SkillDecision.Reject(SkillRejectReason.NotEnoughMana, slot);
        }

        return def.CastType == SkillCastType.AoeTargeting
            ? SkillDecision.StartAoe(slot)
            : SkillDecision.StartInstant(slot);
    }

    private static SkillSlot ResolvePressedSlot(PlayerCommand command)
    {
        if (command.SecondaryPressed)
        {
            return SkillSlot.Secondary;
        }

        if (command.PrimaryPressed)
        {
            return SkillSlot.Primary;
        }

        if (command.Skill1Pressed)
        {
            return SkillSlot.Skill1;
        }

        if (command.Skill2Pressed)
        {
            return SkillSlot.Skill2;
        }

        if (command.Skill3Pressed)
        {
            return SkillSlot.Skill3;
        }

        return SkillSlot.Skill4;
    }
}
