using System;

namespace GodotGameTemplate.Gameplay.Enemies.Feedback;

/// <summary>
/// 敌人攻击前摇循环（纯逻辑）：
/// <para>
/// - 进入攻击意图（玩家贴近）时开始 <see cref="Begin"/> 计时
/// - 前摇期间敌人应停止移动（供拉扯）
/// - 计时到点时由调用方判定玩家是否仍在判定半径内并结算伤害
/// - 结算后进入冷却，冷却结束才可再次起手
/// </para>
/// </summary>
public sealed class EnemyAttackWindup
{
    private double _remaining;
    private double _cooldownRemaining;

    /// <summary>
    /// 前摇时长（秒）。
    /// </summary>
    public double WindupSeconds { get; }

    /// <summary>
    /// 攻击后冷却（秒）。
    /// </summary>
    public double CooldownSeconds { get; }

    /// <summary>
    /// 当前是否处于前摇中（敌人应静止并播放预警表现）。
    /// </summary>
    public bool IsWinding => _remaining > 0d;

    /// <summary>
    /// 是否可以开始一次新的攻击起手。
    /// </summary>
    public bool CanBegin => _remaining <= 0d && _cooldownRemaining <= 0d;

    public EnemyAttackWindup(double windupSeconds = 0.6d, double cooldownSeconds = 0.8d)
    {
        WindupSeconds = windupSeconds;
        CooldownSeconds = cooldownSeconds;
    }

    /// <summary>
    /// 开始一次攻击前摇。
    /// </summary>
    public void Begin()
    {
        if (!CanBegin)
        {
            return;
        }

        _remaining = WindupSeconds;
    }

    /// <summary>
    /// 推进计时；返回本帧是否到达「判定时刻」（应立即结算伤害并进入冷却）。
    /// </summary>
    public bool Tick(double delta)
    {
        if (delta <= 0d)
        {
            return false;
        }

        if (_remaining > 0d)
        {
            _remaining -= delta;
            if (_remaining <= 0d)
            {
                _cooldownRemaining = CooldownSeconds;
                return true;
            }

            return false;
        }

        if (_cooldownRemaining > 0d)
        {
            _cooldownRemaining -= delta;
        }

        return false;
    }

    /// <summary>
    /// 打断当前前摇（例如目标死亡）；不进入冷却。
    /// </summary>
    public void Cancel()
    {
        _remaining = 0d;
    }
}
