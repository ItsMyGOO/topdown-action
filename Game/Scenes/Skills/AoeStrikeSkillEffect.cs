using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;

namespace GodotGameTemplate.Game.Scenes.Skills;

/// <summary>
/// AOE 打击技能效果（最小实现）：
/// <para>
/// - 出场后在下一帧（deferred）做一次范围检测
/// - 对范围内的 <see cref="IHitReceiver"/> 施加一次命中
/// - 执行完销毁自身
/// </para>
/// </summary>
public partial class AoeStrikeSkillEffect : Node2D
{
    [Export]
    public float Radius { get; set; } = 20f;

    [Export]
    public string AttackId { get; set; } = "skill_aoe";

    [Export]
    public int MaxResults { get; set; } = 16;

    public override void _Ready()
    {
        // 延后到空闲帧执行 IntersectShape，避免在物理 flushing queries 阶段报错。
        CallDeferred(nameof(DoStrikeDeferred));
    }

    private void DoStrikeDeferred()
    {
        if (!IsInsideTree())
        {
            QueueFree();
            return;
        }

        var space = GetWorld2D().DirectSpaceState;
        var shape = new CircleShape2D { Radius = Radius };
        var query = new PhysicsShapeQueryParameters2D
        {
            Transform = new Transform2D(0f, GlobalPosition),
            Shape = shape,
            CollideWithAreas = true,
            CollideWithBodies = true,
        };

        var hits = space.IntersectShape(query, maxResults: MaxResults);
        foreach (var hit in hits)
        {
            var collider = hit["collider"].AsGodotObject();
            if (collider is Node2D body && body is IHitReceiver receiver)
            {
                receiver.ReceiveHit(
                    new HitContext(Source: this, Direction: Vector2.Zero, AttackId: AttackId)
                );
            }
        }

        QueueFree();
    }
}
