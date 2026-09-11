using Godot;

namespace GodotGameTemplate.Gameplay.Actors.Combat;

/// <summary>
/// 一次命中的上下文。
/// </summary>
/// <param name="Source">伤害来源节点。</param>
/// <param name="Direction">命中方向。</param>
/// <param name="AttackId">攻击标识。</param>
/// <param name="Damage">本次命中的原始伤害（受方再做抗性减免）。</param>
public readonly record struct HitContext(
    Node Source,
    Vector2 Direction,
    string AttackId,
    int Damage = 1
);
