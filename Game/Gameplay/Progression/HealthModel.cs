using System;

namespace GodotGameTemplate.Gameplay.Progression;

/// <summary>
/// 生命模型（纯逻辑），形状对齐 <see cref="ManaModel"/>：
/// <para>
/// - 维护当前生命（<see cref="Current"/>）与最大生命（<see cref="Max"/>）
/// - 提供按秒缓慢回复（<see cref="Tick"/>）、扣血（<see cref="TakeDamage"/>）与治疗（<see cref="Heal"/>）
/// </para>
/// <para>
/// 注意：该类型不依赖 Godot API，便于单元测试与复用。
/// </para>
/// </summary>
public sealed class HealthModel
{
    /// <summary>
    /// 最大生命值。
    /// </summary>
    public float Max { get; set; } = 100f;

    /// <summary>
    /// 当前生命值。
    /// </summary>
    public float Current { get; private set; } = 100f;

    /// <summary>
    /// 每秒回复的生命值（首版常驻缓慢回复）。
    /// </summary>
    public float RegenPerSecond { get; set; } = 3f;

    /// <summary>
    /// 生命是否已归零（死亡）。
    /// </summary>
    public bool IsEmpty => Current <= 0f;

    /// <summary>
    /// 扣血；负数与零忽略，结果钳制在 0。
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        Current = Math.Max(0f, Current - amount);
    }

    /// <summary>
    /// 治疗；结果钳制在 <see cref="Max"/>。
    /// </summary>
    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        Current = Math.Min(Max, Current + amount);
    }

    /// <summary>
    /// 推进生命回复。
    /// </summary>
    public void Tick(float delta)
    {
        if (delta <= 0f)
        {
            return;
        }

        Current = Math.Min(Max, Current + RegenPerSecond * delta);
    }

    /// <summary>
    /// 上限缩小（如重算装备/等级加成）后，把当前值钳回上限。
    /// </summary>
    public void ClampToMax()
    {
        Current = Math.Min(Max, Current);
    }

    /// <summary>
    /// 将当前生命重置为满值（等于 <see cref="Max"/>）。
    /// </summary>
    public void ResetFull()
    {
        Current = Max;
    }
}
