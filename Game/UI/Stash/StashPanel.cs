using System.Linq;
using Godot;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Player;

namespace GodotGameTemplate.Game.UI.Stash;

/// <summary>
/// 仓库面板：左栏背包（存入），右栏仓库（取出）。
/// </summary>
public partial class StashPanel : PanelContainer
{
    private VBoxContainer _inventoryList = default!;
    private VBoxContainer _stashList = default!;
    private Label _stashHeader = default!;
    private PlayerController? _player;

    public override void _Ready()
    {
        _inventoryList = GetNode<VBoxContainer>("Margin/VBox/Columns/InventorySide/InventoryList");
        _stashList = GetNode<VBoxContainer>("Margin/VBox/Columns/StashSide/StashList");
        _stashHeader = GetNode<Label>("Margin/VBox/Columns/StashSide/StashHeader");
        Refresh();
    }

    public void Refresh()
    {
        ResolvePlayer();

        foreach (var child in _inventoryList.GetChildren().OfType<Node>().ToArray())
        {
            _inventoryList.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var child in _stashList.GetChildren().OfType<Node>().ToArray())
        {
            _stashList.RemoveChild(child);
            child.QueueFree();
        }

        if (_player == null)
        {
            _inventoryList.AddChild(new Label { Text = "未找到玩家" });
            return;
        }

        _stashHeader.Text = $"仓库 {_player.Stash.Items.Count}/{_player.Stash.Capacity}";

        foreach (var item in _player.Inventory.Items)
        {
            var captured = item;
            var row = new HBoxContainer();
            var label = new Label
            {
                Text = $"{item.Id} [{item.Slot}]",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            row.AddChild(label);

            var button = new Button { Text = "存入" };
            button.Pressed += () => OnDepositPressed(captured);
            row.AddChild(button);

            _inventoryList.AddChild(row);
        }

        if (_player.Inventory.Items.Count == 0)
        {
            _inventoryList.AddChild(new Label { Text = "（空）" });
        }

        foreach (var item in _player.Stash.Items)
        {
            var captured = item;
            var row = new HBoxContainer();
            var label = new Label
            {
                Text = $"{item.Id} [{item.Slot}]",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            row.AddChild(label);

            var button = new Button { Text = "取出" };
            button.Pressed += () => OnWithdrawPressed(captured);
            row.AddChild(button);

            _stashList.AddChild(row);
        }

        if (_player.Stash.Items.Count == 0)
        {
            _stashList.AddChild(new Label { Text = "（空）" });
        }
    }

    private void OnDepositPressed(ItemInstance item)
    {
        if (_player == null)
        {
            return;
        }

        StashService.TryDeposit(_player.Inventory, _player.Stash, item);
        _player.SaveProgress();
        Refresh();
    }

    private void OnWithdrawPressed(ItemInstance item)
    {
        if (_player == null)
        {
            return;
        }

        StashService.TryWithdraw(_player.Stash, _player.Inventory, item);
        _player.SaveProgress();
        Refresh();
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
