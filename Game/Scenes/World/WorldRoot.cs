using System.Collections.Generic;
using Godot;
using GodotGameTemplate.Config;
using GodotGameTemplate.Game.Scenes.Items;
using GodotGameTemplate.Gameplay.Enemies;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Player;

namespace GodotGameTemplate.Game.Scenes.World;

/// <summary>
/// 世界根节点：负责把“敌人死亡事件”转换为“掉落物实例化”。
/// </summary>
public partial class WorldRoot : Node2D
{
    private const string TownScenePath = "res://Game/Scenes/Town/Town.tscn";

    [Export]
    public PackedScene? LootPickupScene { get; set; }

    public GameFeatures Features { get; set; } = new();

    private Node? _lootContainer;
    private readonly LootDropper _lootDropper = new();
    private Area2D? _portalToTown;
    private PlayerController? _player;

    public override void _Ready()
    {
        _lootContainer = GetNodeOrNull("YSort") ?? this;
        LootPickupScene ??= GD.Load<PackedScene>("res://Game/Scenes/Items/LootPickup.tscn");
        _portalToTown = GetNodeOrNull<Area2D>("YSort/PortalToTown");

        foreach (var enemy in FindDescendantsOfType<BasicEnemyController>(_lootContainer))
        {
            enemy.Died += OnEnemyDied;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Input.IsActionJustPressed("interact"))
        {
            return;
        }

        if (_portalToTown == null)
        {
            return;
        }

        ResolvePlayer();
        if (_player == null)
        {
            return;
        }

        foreach (var body in _portalToTown.GetOverlappingBodies())
        {
            if (body == _player)
            {
                GetTree().ChangeSceneToFile(TownScenePath);
                return;
            }
        }
    }

    private void OnEnemyDied(Vector2 pos)
    {
        if (!Features.EnableLoot || LootPickupScene == null || _lootContainer == null)
        {
            return;
        }

        var loot = LootPickupScene.Instantiate<LootPickup>();
        loot.GlobalPosition = pos;
        loot.Item = _lootDropper.RollBasicDrop();
        _lootContainer.AddChild(loot);
    }

    private static IEnumerable<T> FindDescendantsOfType<T>(Node root)
        where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match)
            {
                yield return match;
            }

            if (child is Node n)
            {
                foreach (var nested in FindDescendantsOfType<T>(n))
                {
                    yield return nested;
                }
            }
        }
    }

    private void ResolvePlayer()
    {
        if (_player != null && GodotObject.IsInstanceValid(_player))
        {
            return;
        }

        _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
    }
}
