using Godot;

namespace GodotGameTemplate.Gameplay.Enemies;

/// <summary>
/// 敌人 AI 状态。
/// </summary>
public enum EnemyAiState
{
    /// <summary>原地待命。</summary>
    Idle,

    /// <summary>追击玩家。</summary>
    Chase,

    /// <summary>脱战返回出生点。</summary>
    Return,
}

/// <summary>
/// 一次 AI 决策的输入快照。
/// </summary>
/// <param name="Position">敌人当前位置。</param>
/// <param name="HomePosition">敌人出生点（归位目标）。</param>
/// <param name="PlayerPosition">玩家当前位置。</param>
/// <param name="AggroRange">进入追击的警戒半径。</param>
/// <param name="DeaggroRange">脱离追击的半径（应大于警戒半径，形成迟滞）。</param>
/// <param name="LeashRange">距出生点超过此距离强制脱战回家（防风筝）。</param>
/// <param name="PlayerAlive">玩家是否存活。</param>
public readonly record struct EnemyAiSnapshot(
    Vector2 Position,
    Vector2 HomePosition,
    Vector2 PlayerPosition,
    float AggroRange,
    float DeaggroRange,
    float LeashRange,
    bool PlayerAlive
);

/// <summary>
/// 一次 AI 决策的输出。
/// </summary>
/// <param name="State">当前状态。</param>
/// <param name="Direction">归一化移动方向（<see cref="EnemyAiState.Idle"/> 时为零向量）。</param>
/// <param name="ReachedHome">是否已回到出生点附近（调用方用于回满血）。</param>
public readonly record struct EnemyAiDecision(
    EnemyAiState State,
    Vector2 Direction,
    bool ReachedHome
);

/// <summary>
/// 敌人移动 AI 决策器（纯逻辑、无状态）：
/// <para>
/// 规则优先级：玩家死亡 → Idle；距出生点超 leash → Return；
/// 已到家且玩家在脱战圈外 → Idle（报告到家）；
/// 玩家进警戒圈，或在迟滞圈内且未超 leash → Chase；其余 → Idle。
/// </para>
/// </summary>
public sealed class EnemyAiOrchestrator
{
    private const float ArriveThreshold = 4f;

    public EnemyAiDecision Evaluate(EnemyAiSnapshot snapshot)
    {
        if (!snapshot.PlayerAlive)
        {
            return new EnemyAiDecision(EnemyAiState.Idle, Vector2.Zero, false);
        }

        var playerDistance = snapshot.Position.DistanceTo(snapshot.PlayerPosition);
        var homeDistance = snapshot.Position.DistanceTo(snapshot.HomePosition);

        if (homeDistance > snapshot.LeashRange)
        {
            return ReturnHome(snapshot);
        }

        if (homeDistance <= ArriveThreshold && playerDistance > snapshot.DeaggroRange)
        {
            return new EnemyAiDecision(EnemyAiState.Idle, Vector2.Zero, true);
        }

        var shouldChase =
            playerDistance <= snapshot.AggroRange
            || (playerDistance <= snapshot.DeaggroRange && homeDistance <= snapshot.LeashRange);

        if (shouldChase)
        {
            return new EnemyAiDecision(
                EnemyAiState.Chase,
                DirectionTo(snapshot.Position, snapshot.PlayerPosition),
                false
            );
        }

        return new EnemyAiDecision(EnemyAiState.Idle, Vector2.Zero, false);
    }

    private static EnemyAiDecision ReturnHome(EnemyAiSnapshot snapshot)
    {
        return new EnemyAiDecision(
            EnemyAiState.Return,
            DirectionTo(snapshot.Position, snapshot.HomePosition),
            false
        );
    }

    private static Vector2 DirectionTo(Vector2 from, Vector2 to)
    {
        var offset = to - from;

        return offset == Vector2.Zero ? Vector2.Zero : offset.Normalized();
    }
}
