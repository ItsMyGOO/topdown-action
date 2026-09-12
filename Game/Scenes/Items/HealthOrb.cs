using Godot;
using GodotGameTemplate.Gameplay.Player;

namespace GodotGameTemplate.Game.Scenes.Items;

/// <summary>
/// 生命球掉落物：玩家触碰自动拾取，恢复最大生命的 10%（D4 血球范式）。
/// </summary>
public partial class HealthOrb : Area2D
{
    private const float HealFraction = 0.1f;

    public override void _Ready()
    {
        AddToGroup("health_orb");
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not PlayerController player)
        {
            return;
        }

        var health = player.ActorContext.Health;
        health.Heal(health.Max * HealFraction);
        QueueFree();
    }
}
