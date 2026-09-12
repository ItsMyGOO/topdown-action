using System.Linq;
using Godot;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Player;
using GodotGameTemplate.Gameplay.Progression.Classes;
using GodotGameTemplate.Gameplay.Progression.Tiers;
using GodotGameTemplate.Gameplay.Session;

namespace GodotGameTemplate.Game.Scenes.Town;

/// <summary>
/// 城镇控制器：提供最小“售卖/回城传送”闭环验证。
/// - 在 Vendor 区域内按 interact：卖掉背包所有物品换金币，并清空背包
/// - 在 PortalToWorld 区域内按 interact：返回 WorldRoot
/// </summary>
public partial class TownController : Node2D
{
    private const string WorldScenePath = "res://Game/Scenes/World/WorldRoot.tscn";

    private PlayerController? _player;
    private Area2D? _vendorArea;
    private Area2D? _classShrine;
    private Area2D? _tierShrine;
    private Area2D? _portalToDungeon;
    private Area2D? _stashBox;
    private GodotGameTemplate.Game.UI.Stash.StashPanel? _stashPanel;
    private Area2D? _portalToWorld;
    private GameSession? _session;

    public override void _Ready()
    {
        _player = GetNodeOrNull<PlayerController>("YSort/Player");
        _vendorArea = GetNodeOrNull<Area2D>("YSort/Vendor");
        _classShrine = GetNodeOrNull<Area2D>("YSort/ClassShrine");
        _tierShrine = GetNodeOrNull<Area2D>("YSort/TierShrine");
        _portalToDungeon = GetNodeOrNull<Area2D>("YSort/PortalToDungeon");
        _stashBox = GetNodeOrNull<Area2D>("YSort/StashBox");
        _stashPanel = GetNodeOrNull<GodotGameTemplate.Game.UI.Stash.StashPanel>(
            "Hud/Margin/VBox/StashPanel"
        );
        _portalToWorld = GetNodeOrNull<Area2D>("YSort/PortalToWorld");
        _session = GetNodeOrNull<GameSession>("/root/GameSession");
        _session?.Potions.Refill();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Input.IsActionJustPressed("interact"))
        {
            return;
        }

        if (_player == null)
        {
            return;
        }

        if (_portalToWorld != null && IsPlayerInsideArea(_player, _portalToWorld))
        {
            _session?.Save();
            GetTree().ChangeSceneToFile(WorldScenePath);
            return;
        }

        if (_stashBox != null && IsPlayerInsideArea(_player, _stashBox) && _stashPanel != null)
        {
            _stashPanel.Visible = !_stashPanel.Visible;
            if (_stashPanel.Visible)
            {
                _stashPanel.Refresh();
            }

            return;
        }

        if (_portalToDungeon != null && IsPlayerInsideArea(_player, _portalToDungeon))
        {
            _session?.Save();
            GetTree().ChangeSceneToFile("res://Game/Scenes/World/Dungeon.tscn");
            return;
        }

        if (_tierShrine != null && IsPlayerInsideArea(_player, _tierShrine))
        {
            CycleWorldTier();
            return;
        }

        if (_classShrine != null && IsPlayerInsideArea(_player, _classShrine))
        {
            CycleClass();
            return;
        }

        if (_vendorArea != null && IsPlayerInsideArea(_player, _vendorArea))
        {
            SellAllInventoryItems(_player);
        }
    }

    private static void SellAllInventoryItems(PlayerController player)
    {
        var items = player.Inventory.Items.ToArray();
        if (items.Length == 0)
        {
            return;
        }

        var total = 0;
        foreach (var item in items)
        {
            total += GetSellPrice(item);
            player.Inventory.Remove(item);
        }

        player.Gold += total;
    }

    private static int GetSellPrice(ItemInstance item)
    {
        var basePrice = item.Power * 10;

        // 简单“稀有度加成”：魔法 1.5x，稀有 2x。
        return item.Rarity switch
        {
            ItemRarity.Common => basePrice,
            ItemRarity.Magic => basePrice * 3 / 2,
            ItemRarity.Rare => basePrice * 2,
            _ => basePrice,
        };
    }

    /// <summary>
    /// 世界等级石像：循环 普通 → 噩梦 → 地狱，立即存档。
    /// </summary>
    private void CycleWorldTier()
    {
        var catalog = WorldTierDatabase.Catalog;
        var current = WorldTierDatabase.Get(_session.WorldTier).Tier;

        _session.WorldTier = catalog[current % catalog.Count].Tier;
        _session.Save();
    }

    /// <summary>
    /// 职业石像：循环切换职业（单机调试语义），立即存档使选择持久化。
    /// </summary>
    private void CycleClass()
    {
        var catalog = ClassDatabase.Catalog;
        var currentIndex = catalog
            .Select((definition, index) => (definition, index))
            .First(pair => pair.definition.Id == _session.ClassId)
            .index;

        _session.ClassId = catalog[(currentIndex + 1) % catalog.Count].Id;
        _session.Save();
    }

    private static bool IsPlayerInsideArea(PlayerController player, Area2D area)
    {
        foreach (var body in area.GetOverlappingBodies())
        {
            if (body == player)
            {
                return true;
            }
        }

        return false;
    }
}
