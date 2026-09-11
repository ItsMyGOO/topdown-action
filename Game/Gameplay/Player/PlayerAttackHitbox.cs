using System.Collections.Generic;
using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;
using GodotGameTemplate.Gameplay.Player.States;

namespace GodotGameTemplate.Gameplay.Player;

public partial class PlayerAttackHitbox : Area2D, IPlayerAttackHitbox
{
    [Export]
    public CollisionShape2D? CollisionShape { get; set; }

    [Export]
    public Node? SourceNode { get; set; }

    [Export]
    public string AttackId { get; set; } = "player_basic_slash";

    /// <summary>
    /// 每次命中的原始伤害（由玩家控制器按武器 Power 注入）。
    /// </summary>
    [Export]
    public int Damage { get; set; } = 1;

    private Vector2 _facing = Vector2.Down;
    private readonly HashSet<Node> _hitTargets = [];

    public override void _Ready()
    {
        CollisionShape ??= GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        SourceNode ??= GetParent();

        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;
        SetActive(false);
    }

    public void Configure(Vector2 facing, float range)
    {
        _facing = facing == Vector2.Zero ? Vector2.Down : facing.Normalized();
        Position = _facing * range;
    }

    public void SetActive(bool active)
    {
        Monitoring = active;
        if (CollisionShape != null)
        {
            CollisionShape.Disabled = !active;
        }
    }

    public void ResetHitTargets()
    {
        _hitTargets.Clear();
    }

    private void OnBodyEntered(Node2D body)
    {
        TryHit(body);
    }

    private void OnAreaEntered(Area2D area)
    {
        TryHit(area);
    }

    private void TryHit(Node node)
    {
        if (SourceNode == null)
        {
            return;
        }

        if (!TryResolveReceiver(node, out var receiverNode, out var receiver))
        {
            return;
        }

        if (!_hitTargets.Add(receiverNode))
        {
            return;
        }

        receiver.ReceiveHit(new HitContext(SourceNode, _facing, AttackId, Damage));
    }

    private static bool TryResolveReceiver(
        Node startNode,
        out Node receiverNode,
        out IHitReceiver receiver
    )
    {
        Node? current = startNode;
        while (current != null)
        {
            if (current is IHitReceiver hitReceiver)
            {
                receiverNode = current;
                receiver = hitReceiver;
                return true;
            }

            current = current.GetParent();
        }

        receiverNode = null!;
        receiver = null!;
        return false;
    }
}
