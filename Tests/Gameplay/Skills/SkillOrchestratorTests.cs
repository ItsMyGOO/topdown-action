using Godot;
using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Progression;
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Tests.Gameplay.Skills;

public sealed class SkillOrchestratorTests
{
    [Fact]
    public void Evaluate_WhenBusyAndAnySkillPressed_ReturnsRejectBusy()
    {
        var mana = new ManaModel();
        var cd = new CooldownModel();
        var orch = new SkillOrchestrator();

        var cmd = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: Vector2.Zero,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: false,
            Skill1Pressed: true,
            Skill2Pressed: false,
            Skill3Pressed: false,
            Skill4Pressed: false
        );

        var decision = orch.Evaluate(cmd, SkillDatabase.DefaultBySlot, mana, cd, isBusy: true);
        Assert.Equal(SkillDecisionKind.Reject, decision.Kind);
        Assert.Equal(SkillRejectReason.Busy, decision.RejectReason);
    }

    [Fact]
    public void Evaluate_WhenCooldownNotReady_ReturnsRejectCooldown()
    {
        var mana = new ManaModel();
        var cd = new CooldownModel();
        cd.Start(SkillSlot.Skill1, 1f);
        var orch = new SkillOrchestrator();

        var cmd = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: Vector2.Zero,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: false,
            Skill1Pressed: true,
            Skill2Pressed: false,
            Skill3Pressed: false,
            Skill4Pressed: false
        );

        var decision = orch.Evaluate(cmd, SkillDatabase.DefaultBySlot, mana, cd, isBusy: false);
        Assert.Equal(SkillDecisionKind.Reject, decision.Kind);
        Assert.Equal(SkillRejectReason.Cooldown, decision.RejectReason);
        Assert.Equal(SkillSlot.Skill1, decision.Slot);
    }

    [Fact]
    public void Evaluate_WhenManaNotEnough_ReturnsRejectNotEnoughMana()
    {
        var mana = new ManaModel();
        mana.TryConsume(99f);
        var cd = new CooldownModel();
        var orch = new SkillOrchestrator();

        var cmd = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: Vector2.Zero,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: false,
            Skill1Pressed: true,
            Skill2Pressed: false,
            Skill3Pressed: false,
            Skill4Pressed: false
        );

        var decision = orch.Evaluate(cmd, SkillDatabase.DefaultBySlot, mana, cd, isBusy: false);
        Assert.Equal(SkillDecisionKind.Reject, decision.Kind);
        Assert.Equal(SkillRejectReason.NotEnoughMana, decision.RejectReason);
        Assert.Equal(SkillSlot.Skill1, decision.Slot);
    }

    [Fact]
    public void Evaluate_WhenSecondaryPressed_ReturnsStartAoeTargeting()
    {
        var mana = new ManaModel();
        var cd = new CooldownModel();
        var orch = new SkillOrchestrator();

        var cmd = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: Vector2.Zero,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: true,
            Skill1Pressed: true,
            Skill2Pressed: false,
            Skill3Pressed: false,
            Skill4Pressed: false
        );

        var decision = orch.Evaluate(cmd, SkillDatabase.DefaultBySlot, mana, cd, isBusy: false);
        Assert.Equal(SkillDecisionKind.StartAoeTargeting, decision.Kind);
        Assert.Equal(SkillSlot.Secondary, decision.Slot);
    }
}
