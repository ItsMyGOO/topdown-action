using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Skills;

public enum SkillDecisionKind
{
    None = 0,
    StartInstantCast = 1,
    StartAoeTargeting = 2,
    Reject = 3,
}

public enum SkillRejectReason
{
    None = 0,
    Cooldown = 1,
    NotEnoughMana = 2,
    Busy = 3,
}

public readonly record struct SkillDecision(
    SkillDecisionKind Kind,
    SkillSlot Slot,
    SkillRejectReason RejectReason
)
{
    public static SkillDecision None =>
        new(SkillDecisionKind.None, SkillSlot.Primary, SkillRejectReason.None);

    public static SkillDecision StartInstant(SkillSlot slot) =>
        new(SkillDecisionKind.StartInstantCast, slot, SkillRejectReason.None);

    public static SkillDecision StartAoe(SkillSlot slot) =>
        new(SkillDecisionKind.StartAoeTargeting, slot, SkillRejectReason.None);

    public static SkillDecision Reject(SkillRejectReason reason, SkillSlot slot) =>
        new(SkillDecisionKind.Reject, slot, reason);
}
