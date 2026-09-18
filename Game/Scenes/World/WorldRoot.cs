using System;
using System.Collections.Generic;
using Godot;
using GodotGameTemplate.Config;
using GodotGameTemplate.Game.Scenes.Items;
using GodotGameTemplate.Gameplay.Common.Pixel;
using GodotGameTemplate.Gameplay.Enemies;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Player;
using GodotGameTemplate.Gameplay.Progression.Paragon;
using GodotGameTemplate.Gameplay.Progression.Talents;
using GodotGameTemplate.Gameplay.Progression.Tiers;
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

    [Export]
    public PackedScene? HealthOrbScene { get; set; }

    /// <summary>
    /// 地下城模式：运行时铺设敌人波次，杀光全部 Boss 后开启回城门。
    /// </summary>
    [Export]
    public bool DungeonMode { get; set; }

    [Export]
    public float DungeonXpMultiplier { get; set; } = 1.5f;

    public GameFeatures Features { get; set; } = new();

    private const int GroundTilesX = 20;
    private const int GroundTilesY = 12;
    private const int GroundTileSize = 32;
    private const double OrbChanceNormal = 0.30;

    /// <summary>
    /// 地下城小怪伤害缩放（清怪阶段难度调优）。
    /// </summary>
    private const float DungeonTrashDamageScale = 0.7f;
    private const int GroundSeed = 7;

    private Node? _lootContainer;
    private TileMapLayer? _ground;
    private readonly LootDropper _lootDropper = new();
    private readonly Random _eliteRandom = new();
    private readonly Random _orbRandom = new();
    private Area2D? _portalToTown;
    private PlayerController? _player;
    private GameSession? _session;

    public override void _Ready()
    {
        BuildGround();
        _lootContainer = GetNodeOrNull("YSort") ?? this;

        if (DungeonMode)
        {
            SpawnDungeonEnemies();
            BakeDungeonNavigation(excludeGate: true);
        }
        LootPickupScene ??= GD.Load<PackedScene>("res://Game/Scenes/Items/LootPickup.tscn");
        HealthOrbScene ??= GD.Load<PackedScene>("res://Game/Scenes/Items/HealthOrb.tscn");
        _portalToTown = GetNodeOrNull<Area2D>("YSort/PortalToTown");
        _session = GetNodeOrNull<GameSession>("/root/GameSession");

        _dungeonBossTotal = 0;
        _dungeonBossesDown = 0;
        _dungeonTrashTotal = 0;
        _dungeonTrashDown = 0;
        _bossGate = GetNodeOrNull<StaticBody2D>("YSort/BossGate");

        var tier = WorldTierDatabase.Get(_session?.WorldTier ?? 1);

        foreach (var enemy in FindDescendantsOfType<BasicEnemyController>(_lootContainer))
        {
            if (DungeonMode && !enemy.IsBoss)
            {
                _dungeonTrashTotal++;
                // 地下城小怪伤害打折：清怪阶段更宽容。
                enemy.ApplyWorldTier(
                    tier.EnemyHpMultiplier,
                    tier.EnemyDamageMultiplier * DungeonTrashDamageScale
                );
            }
            else
            {
                enemy.ApplyWorldTier(tier.EnemyHpMultiplier, tier.EnemyDamageMultiplier);
            }

            var plan = enemy.IsBoss
                ? EliteRoller.Boss()
                : EliteRoller.TryRoll(EliteRoller.EliteChance, _eliteRandom);
            if (plan != null)
            {
                enemy.ApplyPlan(plan);
            }

            enemy.Died += (pos, xpReward) => OnEnemyDied(enemy, pos, xpReward);

            if (DungeonMode && enemy.IsBoss)
            {
                _dungeonBossTotal++;
            }
        }

        _portalToTown = GetNodeOrNull<Area2D>("YSort/PortalToTown");
        if (DungeonMode && _portalToTown != null)
        {
            _portalToTown.Visible = false;
            _portalToTown.SetDeferred("monitoring", false);
        }
    }

    private int _dungeonBossTotal;
    private int _dungeonBossesDown;
    private int _dungeonTrashTotal;
    private int _dungeonTrashDown;
    private StaticBody2D? _bossGate;
    private bool _wasInteractDown;
    private NavigationRegion2D? _dungeonNavigation;

    /// <summary>
    /// 地下城敌人铺设：网格排布 18 普通怪 + 3 Boss。
    /// </summary>
    private void SpawnDungeonEnemies()
    {
        var enemyScene = GD.Load<PackedScene>("res://Game/Scenes/Enemies/BasicEnemy.tscn");
        var container = GetNodeOrNull("YSort") ?? this;

        // 小怪区（x < 420）：10 只稀疏分布（难度调优：数量与密度下调）。
        for (var i = 0; i < 10; i++)
        {
            var enemy = enemyScene.Instantiate<BasicEnemyController>();
            enemy.Position = new Vector2(140f + (i % 5) * 55f, 90f + (i / 5) * 75f);
            container.AddChild(enemy);
        }

        // Boss 房（x > 420）：3 只。
        for (var i = 0; i < 3; i++)
        {
            var boss = enemyScene.Instantiate<BasicEnemyController>();
            boss.IsBoss = true;
            boss.Position = new Vector2(480f + i * 60f, 190f);
            container.AddChild(boss);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateDungeonPrompt();
        UpdateInteractPrompt();

        var interactDown = Input.IsMouseButtonPressed(MouseButton.Left);
        var interactClicked = interactDown && !_wasInteractDown;
        _wasInteractDown = interactDown;

        if (!interactClicked && !Input.IsActionJustPressed("interact"))
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

    /// <summary>
    /// 生成像素瓦片地面：铺满相机边界覆盖的世界范围，绘制在 YSort 之下。
    /// </summary>
    private void BuildGround()
    {
        if (_ground != null)
        {
            return;
        }

        var tileSet = GD.Load<TileSet>("res://Game/Art/Pixel/tileset.tres");
        _ground = new TileMapLayer { TileSet = tileSet };
        AddChild(_ground);
        MoveChild(_ground, 0);

        var layout = GroundLayout.Generate(GroundTilesX, GroundTilesY, GroundSeed, variants: 3);
        for (var x = 0; x < GroundTilesX; x++)
        {
            for (var y = 0; y < GroundTilesY; y++)
            {
                _ground.SetCell(new Vector2I(x, y), 0, new Vector2I(layout[x, y], 0));
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
            var tier = WorldTierDatabase.Get(_session.WorldTier);
            var xp = (int)(
                xpReward
                * equipStats.XpMultiplier
                * talentStats.XpMultiplier
                * tier.XpMultiplier
                * (DungeonMode ? DungeonXpMultiplier : 1f)
            );
            _session.Leveling.AddXp(xp);
        }

        if (plan != null && Features.EnableLoot && _session != null)
        {
            _session.Gold += plan.GoldBonus;
        }

        var dropCount = plan?.DropCount ?? 1;
        var tierFloor = WorldTierDatabase.Get(_session?.WorldTier ?? 1).DropRarityFloor;
        var rarityFloor = (ItemRarity)
            Math.Max((int)(plan?.RarityFloor ?? ItemRarity.Common), (int)tierFloor);

        if (DungeonMode && enemy.IsBoss)
        {
            _dungeonBossesDown++;
            if (_dungeonBossesDown >= _dungeonBossTotal)
            {
                CallDeferred(nameof(OpenDungeonExit));
            }
        }
        else if (DungeonMode)
        {
            _dungeonTrashDown++;
            if (_dungeonTrashDown >= _dungeonTrashTotal)
            {
                CallDeferred(nameof(OpenBossGate));
            }
        }

        // 血球：普通怪 30%，精英必掉 1，Boss 2。
        var orbCount = plan switch
        {
            { IsBoss: true } => 2,
            { Affix: not EliteAffix.None } => 1,
            _ when _orbRandom.NextDouble() < OrbChanceNormal => 1,
            _ => 0,
        };

        // 该信号可能来自物理回调（例如 Area2D.BodyEntered）期间。
        // 在 flushing queries 阶段直接 AddChild/改监测状态会触发引擎报错：
        // "Can't change this state while flushing queries."
        // 因此这里统一延后到空闲帧再生成掉落。
        CallDeferred(nameof(SpawnLootDeferred), pos, dropCount, (int)rarityFloor, orbCount);
    }

    /// <summary>
    /// 运行时烘焙地下城导航网格（墙体为障碍；门按需排除）。
    /// </summary>
    private void BakeDungeonNavigation(bool excludeGate)
    {
        var navigationPolygon = new NavigationPolygon
        {
            // 默认 ParsedGeometryType 是 MeshInstances；墙体是 StaticBody2D，必须显式切到 StaticColliders。
            ParsedGeometryType = NavigationPolygon.ParsedGeometryTypeEnum.StaticColliders,
            AgentRadius = 8f,
        };

        // 可走区域 outline（整图外框）；parse 收集的墙体障碍将从中裁剪。
        // 4.6 C#：outline 为 Vector2[] 参数。
        navigationPolygon.AddOutline([
            new Vector2(0f, 0f),
            new Vector2(640f, 0f),
            new Vector2(640f, 380f),
            new Vector2(0f, 380f),
        ]);
        var sourceGeometry = new NavigationMeshSourceGeometryData2D();

        var root = GetNodeOrNull("YSort") ?? this;

        // 门永远排除在烘焙外：导航上门洞恒可走，实际通行由门的物理开关控制。
        Node? gateHolder = null;
        if (excludeGate && _bossGate?.GetParent() != null)
        {
            gateHolder = _bossGate.GetParent();
            gateHolder.RemoveChild(_bossGate);
        }

        NavigationServer2D.ParseSourceGeometryData(
            navigationPolygon,
            sourceGeometry,
            root,
            Callable.From(() => { })
        );
        NavigationServer2D.BakeFromSourceGeometryData(navigationPolygon, sourceGeometry);

        gateHolder?.AddChild(_bossGate!);

        if (_dungeonNavigation == null)
        {
            _dungeonNavigation = new NavigationRegion2D();
            AddChild(_dungeonNavigation);
        }

        _dungeonNavigation.NavigationPolygon = navigationPolygon;
    }

    /// <summary>
    /// 小怪清空后开启 Boss 房门。
    /// </summary>
    private void OpenBossGate()
    {
        if (_bossGate == null)
        {
            return;
        }

        foreach (var child in _bossGate.GetChildren())
        {
            if (child is CollisionPolygon2D polygon)
            {
                polygon.SetDeferred("disabled", true);
            }
        }

        _bossGate.Visible = false;

        // 门洞亮绿色通道：明确告知玩家从哪里进。
        GetNodeOrNull<Polygon2D>("YSort/GateOpenMarker")?.SetDeferred("visible", true);
    }

    /// <summary>
    /// 地下城进度提示（复用交互提示标签）：清怪进度 → Boss 房 → 回城门。
    /// </summary>
    private void UpdateDungeonPrompt()
    {
        if (!DungeonMode)
        {
            return;
        }

        var label = GetTree().GetFirstNodeInGroup("interact_prompt") as Label;
        if (label == null)
        {
            return;
        }

        string text;
        if (_dungeonTrashDown < _dungeonTrashTotal)
        {
            text = $"清理小怪 {_dungeonTrashDown}/{_dungeonTrashTotal} → 开启 Boss 房门";
        }
        else if (_dungeonBossesDown < _dungeonBossTotal)
        {
            text =
                $"Boss 房已开启！击败 Boss {_dungeonBossesDown}/{_dungeonBossTotal} → 开启回城门";
        }
        else
        {
            text = string.Empty;
        }

        label.Visible = text != string.Empty;
        if (label.Visible)
        {
            label.Text = text;
        }
    }

    /// <summary>
    /// 地下城全部 Boss 死亡后开启回城门。
    /// </summary>
    private void OpenDungeonExit()
    {
        if (_portalToTown == null)
        {
            return;
        }

        _portalToTown.Visible = true;
        _portalToTown.SetDeferred("monitoring", true);
    }

    private void SpawnLootDeferred(Vector2 pos, int dropCount, int rarityFloor, int orbCount)
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

        if (HealthOrbScene == null)
        {
            return;
        }

        for (var i = 0; i < orbCount; i++)
        {
            var orb = HealthOrbScene.Instantiate<HealthOrb>();
            orb.GlobalPosition = pos + new Vector2(-8f + i * 16f, 8f);
            _lootContainer.AddChild(orb);
        }
    }

    /// <summary>
    /// 靠近回城门时显示交互提示（地下城回城门初始隐藏时不提示）。
    /// </summary>
    private void UpdateInteractPrompt()
    {
        ResolvePlayer();

        var label = GetTree().GetFirstNodeInGroup("interact_prompt") as Label;
        if (label == null || _player == null || _portalToTown == null || !_portalToTown.Visible)
        {
            return;
        }

        var near = false;
        foreach (var body in _portalToTown.GetOverlappingBodies())
        {
            if (body == _player)
            {
                near = true;
                break;
            }
        }

        if (near)
        {
            // 仅在站上出口时覆盖进度文本；离开时交回 UpdateDungeonPrompt 管理。
            label.Visible = true;
            label.Text = "左键点击交互：返回城镇";
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
