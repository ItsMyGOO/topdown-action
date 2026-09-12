using System;

namespace GodotGameTemplate.Gameplay.Progression;

/// <summary>
/// 闪避充能模型（纯逻辑，对齐 D4：无体力，闪避为可积累次数 + 自动回复）：
/// <para>
/// - 内部使用浮点池：<see cref="Available"/> 为整数充能数，小数部分是进行中的回复
/// - <see cref="Tick"/> 按 <see cref="RechargeSeconds"/> 每格回复，满值钳制
/// </para>
/// </summary>
public sealed class EvadeChargesModel
{
    private const float MinRechargeSeconds = 0.1f;

    private float _pool;

    /// <summary>
    /// 最大充能数。
    /// </summary>
    public int MaxCharges { get; }

    /// <summary>
    /// 每回复一格所需秒数的基准值（不变，供天赋做减法）。
    /// </summary>
    public float RechargeSecondsBase { get; }

    /// <summary>
    /// 当前生效的每格回复秒数（天赋缩短后的值）。
    /// </summary>
    public float RechargeSeconds { get; set; }

    public EvadeChargesModel(int maxCharges = 2, float rechargeSeconds = 5f)
    {
        MaxCharges = Math.Max(1, maxCharges);
        RechargeSecondsBase = MathF.Max(MinRechargeSeconds, rechargeSeconds);
        RechargeSeconds = RechargeSecondsBase;
        _pool = MaxCharges;
    }

    /// <summary>
    /// 当前可用（已充满）的充能数。
    /// </summary>
    public int Available => (int)MathF.Floor(_pool);

    /// <summary>
    /// 消耗一格充能；不可用时返回 false。
    /// </summary>
    public bool TryConsume()
    {
        if (Available < 1)
        {
            return false;
        }

        _pool -= 1f;
        return true;
    }

    /// <summary>
    /// 推进回复；非正间隔忽略。
    /// </summary>
    public void Tick(float delta)
    {
        if (delta <= 0f || _pool >= MaxCharges)
        {
            return;
        }

        _pool = MathF.Min(
            MaxCharges,
            _pool + delta / MathF.Max(MinRechargeSeconds, RechargeSeconds)
        );
    }
}
