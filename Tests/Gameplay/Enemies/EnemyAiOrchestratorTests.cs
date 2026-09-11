using Godot;
using GodotGameTemplate.Gameplay.Enemies;

namespace GodotGameTemplate.Tests.Gameplay.Enemies;

public sealed class EnemyAiOrchestratorTests
{
    private static EnemyAiSnapshot Snapshot(
        Vector2 position,
        Vector2 playerPosition,
        bool playerAlive = true,
        Vector2? home = null
    )
    {
        return new EnemyAiSnapshot(
            position,
            home ?? Vector2.Zero,
            playerPosition,
            AggroRange: 140f,
            DeaggroRange: 175f,
            LeashRange: 260f,
            PlayerAlive: playerAlive
        );
    }

    [Fact]
    public void Evaluate_PlayerFarBeyondDeaggro_IdlesAtHome()
    {
        var decision = new EnemyAiOrchestrator().Evaluate(
            Snapshot(new Vector2(0, 0), new Vector2(300, 0))
        );

        Assert.Equal(EnemyAiState.Idle, decision.State);
        Assert.Equal(Vector2.Zero, decision.Direction);
        Assert.True(decision.ReachedHome);
    }

    [Fact]
    public void Evaluate_PlayerWithinAggro_ChasesTowardPlayer()
    {
        var decision = new EnemyAiOrchestrator().Evaluate(
            Snapshot(new Vector2(0, 0), new Vector2(100, 0))
        );

        Assert.Equal(EnemyAiState.Chase, decision.State);
        Assert.Equal(Vector2.Right, decision.Direction);
    }

    [Fact]
    public void Evaluate_PlayerBetweenAggroAndDeaggro_KeepsChasing()
    {
        // 迟滞：脱战半径(175) > 警戒半径(140)，避免边界抖动。
        var decision = new EnemyAiOrchestrator().Evaluate(
            Snapshot(new Vector2(0, 0), new Vector2(160, 0))
        );

        Assert.Equal(EnemyAiState.Chase, decision.State);
    }

    [Fact]
    public void Evaluate_PulledBeyondLeash_ReturnsHome()
    {
        var decision = new EnemyAiOrchestrator().Evaluate(
            Snapshot(new Vector2(300, 0), new Vector2(500, 0))
        );

        Assert.Equal(EnemyAiState.Return, decision.State);
        Assert.Equal(Vector2.Left, decision.Direction);
    }

    [Fact]
    public void Evaluate_BackHomeAndPlayerAway_IdlesAndReportsReachedHome()
    {
        var decision = new EnemyAiOrchestrator().Evaluate(
            Snapshot(new Vector2(2, 0), new Vector2(400, 0))
        );

        Assert.Equal(EnemyAiState.Idle, decision.State);
        Assert.True(decision.ReachedHome);
    }

    [Fact]
    public void Evaluate_PlayerBeyondDeaggro_ButWithinItFromAggroRing_Idles()
    {
        // 玩家在警戒圈外、脱战圈外，敌人未离家 → Idle 且不算到家。
        var decision = new EnemyAiOrchestrator().Evaluate(
            Snapshot(new Vector2(100, 0), new Vector2(280, 0))
        );

        Assert.Equal(EnemyAiState.Idle, decision.State);
        Assert.False(decision.ReachedHome);
    }

    [Fact]
    public void Evaluate_PlayerDead_AlwaysIdles()
    {
        var decision = new EnemyAiOrchestrator().Evaluate(
            Snapshot(new Vector2(0, 0), new Vector2(50, 0), playerAlive: false)
        );

        Assert.Equal(EnemyAiState.Idle, decision.State);
        Assert.Equal(Vector2.Zero, decision.Direction);
    }

    [Fact]
    public void Evaluate_ChaseDirection_IsNormalized()
    {
        var decision = new EnemyAiOrchestrator().Evaluate(
            Snapshot(new Vector2(100, 100), new Vector2(150, 160))
        );

        Assert.Equal(EnemyAiState.Chase, decision.State);
        Assert.Equal(1f, decision.Direction.Length(), 3);
    }
}
