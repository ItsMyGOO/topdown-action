using System.Collections.Generic;
using Godot;
using GodotGameTemplate.Config;
using GodotGameTemplate.Gameplay.Enemies;
using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Game.Scenes.World;

/// <summary>
/// 世界根节点：负责把“敌人死亡事件”转换为“掉落物实例化”。
/// </summary>
public partial class WorldRoot : Node2D
{
    [Export]
    public PackedScene? LootPickupScene { get; set; }

    public GameFeatures Features { get; set; } = new();

    private Node? _lootContainer;

    public override void _Ready()
    {
        _lootContainer = GetNodeOrNull("YSort") ?? this;
        LootPickupScene ??= GD.Load<PackedScene>("res://Game/Scenes/Items/LootPickup.tscn");

        foreach (var enemy in FindDescendantsOfType<BasicEnemyController>(_lootContainer))
        {
            enemy.Died += OnEnemyDied;
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
        loot.ItemId = "gold";
        loot.Quantity = 1;
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
}
