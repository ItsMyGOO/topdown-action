using System;
using System.Collections.Generic;
using Godot;
using GodotGameTemplate.Config;
using GodotGameTemplate.Game.Scenes.Items;
using GodotGameTemplate.Gameplay.Enemies;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Player;
using GodotGameTemplate.Gameplay.Progression.Talents;
using GodotGameTemplate.Gameplay.Session;

namespace GodotGameTemplate.Game.Scenes.World;

/// <summary>
/// 世界根节点：负责把“敌人死亡事件”转换为“掉落物实例化”，
/// 并在生成期掷取精英/Boss 强化计划。
/// </summary>
public partial class WorldRoot : Node2D
{
    private const string TownScenePath = "res://Game/Scenes/Town/Town.tscn";

    [Export]
    public PackedScene? LootPickupScene { get; set; }

    public GameFeatures Features { get; set; } = new();

    private Node? _lootContainer;
    private readonly LootDropper _lootDropper = new();
    private readonly Random _eliteRandom = new();
    private Area2D? _portalToTown;
    private PlayerController? _player;
    private GameSession? _session;

    public override void _Ready()
    {
        _lootContainer = GetNodeOrNull("YSort") ?? this;
        LootPickupScene ??= GD.Load<PackedScene>("res://Game/Scenes/Items/LootPickup.tscn");
        _portalToTown = GetNodeOrNull<Area2D>("YSort/PortalToTown");
        _session = GetNodeOrNull<GameSession>("/root/GameSession");

        foreach (var enemy in FindDescendantsOfType<BasicEnemyController>(_lootContainer))
        {
            var plan = enemy.IsBoss
                ? EliteRoller.Boss()
                : EliteRoller.TryRoll(EliteRoller.EliteChance, _eliteRandom);
            if (plan != null)
            {
                enemy.ApplyPlan(plan);
            }

            enemy.Died += (pos, xpReward) => OnEnemyDied(enemy, pos, xpReward);
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
                _session?.Save();
                GetTree().ChangeSceneToFile(TownScenePath);
                return;
            }
        }
    }

    private void OnEnemyDied(BasicEnemyController enemy, Vector2 pos, int xpReward)
    {
        var plan = enemy.Plan;

        if (Features.EnableLeveling && _session != null)
        {
            var equipStats = EquipmentStats.Summarize(_session.Equipment);
            var talentStats = TalentStats.Aggregate(_session.Talents);
            var xp = (int)(xpReward * equipStats.XpMultiplier * talentStats.XpMultiplier);
            _session.Leveling.AddXp(xp);
        }

        if (plan != null && Features.EnableLoot && _session != null)
        {
            _session.Gold += plan.GoldBonus;
        }

        var dropCount = plan?.DropCount ?? 1;
        var rarityFloor = plan?.RarityFloor ?? ItemRarity.Common;

        // 该信号可能来自物理回调（例如 Area2D.BodyEntered）期间。
        // 在 flushing queries 阶段直接 AddChild/改监测状态会触发引擎报错：
        // "Can't change this state while flushing queries."
        // 因此这里统一延后到空闲帧再生成掉落。
        CallDeferred(nameof(SpawnLootDeferred), pos, dropCount, (int)rarityFloor);
    }

    private void SpawnLootDeferred(Vector2 pos, int dropCount, int rarityFloor)
    {
        if (!Features.EnableLoot || LootPickupScene == null || _lootContainer == null)
        {
            return;
        }

        for (var i = 0; i < dropCount; i++)
        {
            var loot = LootPickupScene.Instantiate<LootPickup>();
            loot.GlobalPosition = pos + new Vector2(i * 10f, 0);
            loot.Item = _lootDropper.RollDrop((ItemRarity)rarityFloor);
            _lootContainer.AddChild(loot);
        }
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
