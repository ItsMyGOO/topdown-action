namespace GodotGameTemplate.Gameplay.Combat;

/// <summary>
/// 战斗编排器（纯逻辑）：
/// 根据目标有效性、距离以及角色当前状态，决定当前帧应该清目标、追击还是触发攻击。
/// </summary>
public sealed class CombatOrchestrator
{
    /// <summary>
    /// 进入普攻的距离阈值。
    /// </summary>
    public float AttackRange { get; set; } = 18f;

    /// <summary>
    /// 允许持续追击目标的最大距离。
    /// 超出后应视为脱战并清空目标。
    /// </summary>
    public float ChaseMaxDistance { get; set; } = 240f;

    /// <summary>
    /// 评估当前帧的战斗决策。
    /// </summary>
    public CombatDecision Evaluate(CombatSnapshot snapshot)
    {
        if (!snapshot.HasTarget)
        {
            return CombatDecision.None;
        }

        if (!snapshot.IsTargetValid || snapshot.DistanceToTarget > ChaseMaxDistance)
        {
            return new CombatDecision(ClearTarget: true, ShouldChase: false, ShouldAttack: false);
        }

        if (snapshot.DistanceToTarget > AttackRange)
        {
            return new CombatDecision(
                ClearTarget: false,
                ShouldChase: !snapshot.IsAttacking,
                ShouldAttack: false
            );
        }

        if (!snapshot.IsAttacking && snapshot.CanAttack)
        {
            return new CombatDecision(ClearTarget: false, ShouldChase: false, ShouldAttack: true);
        }

        return CombatDecision.None;
    }
}

/// <summary>
/// 战斗编排输入快照。
/// </summary>
public readonly record struct CombatSnapshot(
    bool HasTarget,
    bool IsTargetValid,
    float DistanceToTarget,
    bool IsAttacking,
    bool CanAttack
);

/// <summary>
/// 战斗编排输出结果。
/// </summary>
public readonly record struct CombatDecision(bool ClearTarget, bool ShouldChase, bool ShouldAttack)
{
    /// <summary>
    /// 不执行任何额外动作的空决策。
    /// </summary>
    public static CombatDecision None => new(false, false, false);
}
