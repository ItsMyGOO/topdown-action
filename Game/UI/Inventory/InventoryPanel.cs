using System.Linq;
using Godot;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Player;

namespace GodotGameTemplate.Game.UI.Inventory;

/// <summary>
/// 最小背包面板：展示 <see cref="InventoryModel.Items"/>，并提供“装备”按钮。
/// </summary>
public partial class InventoryPanel : PanelContainer
{
    private VBoxContainer _itemsList = default!;
    private PlayerController? _player;

    public override void _Ready()
    {
        _itemsList = GetNode<VBoxContainer>("Margin/VBox/Scroll/ItemsList");
        Refresh();
    }

    public void Refresh()
    {
        ResolvePlayer();

        foreach (var child in _itemsList.GetChildren().OfType<Node>().ToArray())
        {
            // 立即摘除再延迟释放：避免同帧重复 Refresh 时旧行仍在树上造成重复。
            _itemsList.RemoveChild(child);
            child.QueueFree();
        }

        if (_player == null)
        {
            _itemsList.AddChild(new Label { Text = "未找到玩家（group: player）" });
            return;
        }

        if (_player.Inventory.Items.Count == 0)
        {
            _itemsList.AddChild(new Label { Text = "（空）" });
            return;
        }

        foreach (var item in _player.Inventory.Items)
        {
            var captured = item;

            var row = new HBoxContainer();

            var text = $"{captured.Id}  [{captured.Slot}]  P{captured.Power}";
            if (captured.Affixes.Length > 0)
            {
                text += $"\n{AffixText.FormatAll(captured.Affixes)}";
            }

            var label = new Label { Text = text };
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(label);

            var equipBtn = new Button { Text = "装备" };
            equipBtn.Pressed += () => OnEquipPressed(captured);
            row.AddChild(equipBtn);

            _itemsList.AddChild(row);
        }
    }

    private void OnEquipPressed(ItemInstance item)
    {
        if (_player == null)
        {
            return;
        }

        InventoryEquipmentService.TryEquip(_player.Inventory, _player.Equipment, item);
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
