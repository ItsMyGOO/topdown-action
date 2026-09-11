using System;

namespace GodotGameTemplate.Gameplay.Enemies.Feedback;

/// <summary>
/// 击退冲量模型（纯逻辑，Godot 无关）：
/// <para>
/// - <see cref="Apply"/> 以「覆盖式」写入方向 × 冲量（连续受击不叠加成失控）
/// - <see cref="Tick"/> 按指数衰减，使击退在短时间后自然停下
/// </para>
/// </summary>
public sealed class KnockbackModel
{
    private const float DecayRate = 8f;

    /// <summary>当前击退速度 X 分量。</summary>
    public float X { get; private set; }

    /// <summary>当前击退速度 Y 分量。</summary>
    public float Y { get; private set; }

    /// <summary>
    /// 沿给定方向（应为单位向量）施加击退冲量。
    /// </summary>
    public void Apply(float dirX, float dirY, float impulse)
    {
        X = dirX * impulse;
        Y = dirY * impulse;
    }

    /// <summary>
    /// 推进衰减；非正间隔忽略。
    /// </summary>
    public void Tick(float delta)
    {
        if (delta <= 0f)
        {
            return;
        }

        var decay = MathF.Exp(-DecayRate * delta);
        X *= decay;
        Y *= decay;
    }
}
