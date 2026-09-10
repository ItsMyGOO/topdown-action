using Godot;
using GodotGameTemplate.Game.UI.Inventory;
using GodotGameTemplate.Gameplay.Player;

namespace GodotGameTemplate.Game.UI.Hud;

/// <summary>
/// 最小 HUD：显示体力与金币占位，并监听 <c>open_inventory</c> 切换背包面板。
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _staminaLabel = default!;
    private Label _goldLabel = default!;
    private InventoryPanel _inventoryPanel = default!;

    private PlayerController? _player;

    public override void _Ready()
    {
        _staminaLabel = GetNode<Label>("Margin/VBox/StaminaLabel");
        _goldLabel = GetNode<Label>("Margin/VBox/GoldLabel");
        _inventoryPanel = GetNode<InventoryPanel>("Margin/VBox/InventoryPanel");
    }

    public override void _Process(double delta)
    {
        ResolvePlayer();
        UpdateHudText();

        if (Input.IsActionJustPressed("open_inventory"))
        {
            _inventoryPanel.Visible = !_inventoryPanel.Visible;
            if (_inventoryPanel.Visible)
            {
                _inventoryPanel.Refresh();
            }
        }
    }

    private void UpdateHudText()
    {
        if (_player == null)
        {
            _staminaLabel.Text = "体力: --/--";
            _goldLabel.Text = "金币: 0";
            return;
        }

        var stamina = _player.ActorContext.Stamina;
        _staminaLabel.Text = $"体力: {stamina.Current:0}/{stamina.Max:0}";
        _goldLabel.Text = $"金币: {_player.Gold}";
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
