using System;

namespace GodotGameTemplate.Gameplay.Progression;

/// <summary>
/// 生命药水充能模型（纯逻辑，D4 式：独立于背包的固定充能池）：
/// 回城补满，用 Q 消耗。
/// </summary>
public sealed class PotionChargesModel
{
    /// <summary>
    /// 最大充能数。
    /// </summary>
    public int MaxCharges { get; }

    /// <summary>
    /// 当前可用充能数。
    /// </summary>
    public int Available { get; private set; }

    public PotionChargesModel(int maxCharges = 4)
    {
        MaxCharges = Math.Max(1, maxCharges);
        Available = MaxCharges;
    }

    /// <summary>
    /// 消耗一格；不可用时返回 false。
    /// </summary>
    public bool TryConsume()
    {
        if (Available < 1)
        {
            return false;
        }

        Available--;
        return true;
    }

    /// <summary>
    /// 补满全部充能（回城补给）。
    /// </summary>
    public void Refill()
    {
        Available = MaxCharges;
    }

    /// <summary>
    /// 从存档恢复；非法值钳制到 0..<see cref="MaxCharges"/>。
    /// </summary>
    public void Restore(int charges)
    {
        Available = Math.Clamp(charges, 0, MaxCharges);
    }
}
