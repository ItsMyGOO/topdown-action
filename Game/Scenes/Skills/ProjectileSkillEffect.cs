using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;

namespace GodotGameTemplate.Game.Scenes.Skills;

/// <summary>
/// 投射物技能效果（最小实现）：
/// <para>
/// - 以固定速度沿 <see cref="Direction"/> 飞行
/// - 命中 <see cref="IHitReceiver"/> 时造成一次命中并销毁
/// </para>
/// </summary>
public partial class ProjectileSkillEffect : Area2D
{
    [Export]
    public float Speed { get; set; } = 220f;

    [Export]
    public float LifetimeSeconds { get; set; } = 1.2f;

    [Export]
    public string AttackId { get; set; } = "skill_projectile";

    /// <summary>
    /// 命中造成的原始伤害（由施放方注入玩家攻击力）。
    /// </summary>
    [Export]
    public int Damage { get; set; } = 1;

    /// <summary>
    /// 施放者（命中判定时忽略，避免出生即撞到玩家自毁）。
    /// </summary>
    public Node2D? Source { get; set; }

    /// <summary>
    /// 飞行方向（世界空间，建议为单位向量）。
    /// </summary>
    public Vector2 Direction { get; set; } = Vector2.Right;

    private double _elapsed;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        _elapsed += delta;
        if (_elapsed >= LifetimeSeconds)
        {
            QueueFree();
            return;
        }

        GlobalPosition += Direction.Normalized() * Speed * (float)delta;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body == Source)
        {
            return;
        }

        if (body is IHitReceiver receiver)
        {
            receiver.ReceiveHit(new HitContext(this, Direction, AttackId, Damage));
        }

        QueueFree();
    }
}
