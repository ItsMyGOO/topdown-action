using System;

namespace GodotGameTemplate.Gameplay.Progression;

/// <summary>
/// 体力模型（纯逻辑）：
/// <para>
/// - 维护当前体力（<see cref="Current"/>）与最大体力（<see cref="Max"/>）
/// - 提供按秒恢复（<see cref="Tick"/>）与消耗（<see cref="TryConsume"/>）
/// </para>
/// <para>
/// 注意：该类型不依赖 Godot API，便于单元测试与复用。
/// </para>
/// </summary>
public sealed class StaminaModel
{
    /// <summary>
    /// 最大体力值。
    /// </summary>
    public float Max { get; set; } = 100f;

    /// <summary>
    /// 当前体力值。
    /// </summary>
    public float Current { get; private set; } = 100f;

    /// <summary>
    /// 每秒恢复的体力值。
    /// </summary>
    public float RegenPerSecond { get; set; } = 20f;

    /// <summary>
    /// 将当前体力重置为满值（等于 <see cref="Max"/>）。
    /// </summary>
    public void ResetFull()
    {
        Current = Max;
    }

    /// <summary>
    /// 推进体力恢复。
    /// </summary>
    /// <param name="delta">
    /// 帧间隔（秒）。通常传入 <c>(float)delta</c>。
    /// </param>
    public void Tick(float delta)
    {
        if (delta <= 0f)
        {
            return;
        }

        Current = MathF.Min(Max, Current + RegenPerSecond * delta);
    }

    /// <summary>
    /// 尝试消耗体力。
    /// </summary>
    /// <param name="amount">消耗量。</param>
    /// <returns>
    /// 若体力充足则返回 <c>true</c> 并扣除体力；否则返回 <c>false</c>，不做扣减。
    /// </returns>
    public bool TryConsume(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (Current < amount)
        {
            return false;
        }

        Current -= amount;
        return true;
    }
}
