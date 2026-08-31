using GodotGameTemplate.Gameplay.Combat;

namespace GodotGameTemplate.Tests.Gameplay.Combat;

public sealed class CombatOrchestratorTests
{
    [Fact]
    public void Evaluate_WhenTargetIsInvalid_ReturnsClearTarget()
    {
        var orchestrator = new CombatOrchestrator();

        var decision = orchestrator.Evaluate(
            new CombatSnapshot(
                HasTarget: true,
                IsTargetValid: false,
                DistanceToTarget: 0f,
                IsAttacking: false,
                CanAttack: true
            )
        );

        Assert.True(decision.ClearTarget);
        Assert.False(decision.ShouldChase);
        Assert.False(decision.ShouldAttack);
    }

    [Fact]
    public void Evaluate_WhenTargetIsBeyondChaseRange_ReturnsClearTarget()
    {
        var orchestrator = new CombatOrchestrator { ChaseMaxDistance = 120f };

        var decision = orchestrator.Evaluate(
            new CombatSnapshot(
                HasTarget: true,
                IsTargetValid: true,
                DistanceToTarget: 121f,
                IsAttacking: false,
                CanAttack: true
            )
        );

        Assert.True(decision.ClearTarget);
        Assert.False(decision.ShouldChase);
        Assert.False(decision.ShouldAttack);
    }

    [Fact]
    public void Evaluate_WhenTargetIsOutOfAttackRange_ReturnsChase()
    {
        var orchestrator = new CombatOrchestrator { AttackRange = 20f, ChaseMaxDistance = 200f };

        var decision = orchestrator.Evaluate(
            new CombatSnapshot(
                HasTarget: true,
                IsTargetValid: true,
                DistanceToTarget: 48f,
                IsAttacking: false,
                CanAttack: true
            )
        );

        Assert.False(decision.ClearTarget);
        Assert.True(decision.ShouldChase);
        Assert.False(decision.ShouldAttack);
    }

    [Fact]
    public void Evaluate_WhenTargetIsInAttackRangeAndActorIsReady_ReturnsAttack()
    {
        var orchestrator = new CombatOrchestrator { AttackRange = 20f, ChaseMaxDistance = 200f };

        var decision = orchestrator.Evaluate(
            new CombatSnapshot(
                HasTarget: true,
                IsTargetValid: true,
                DistanceToTarget: 18f,
                IsAttacking: false,
                CanAttack: true
            )
        );

        Assert.False(decision.ClearTarget);
        Assert.False(decision.ShouldChase);
        Assert.True(decision.ShouldAttack);
    }
}
