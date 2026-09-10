using Godot;
using GodotGameTemplate.Game.UI.Inventory;
using GodotGameTemplate.Gameplay.Player;
using GodotGameTemplate.Gameplay.Skills;
using SkillSlot = GodotGameTemplate.Gameplay.Input.Commands.SkillSlot;

namespace GodotGameTemplate.Game.UI.Hud;

/// <summary>
/// 最小 HUD：显示体力与金币占位，并监听 <c>open_inventory</c> 切换背包面板。
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _staminaLabel = default!;
    private Label _manaLabel = default!;
    private Label _goldLabel = default!;
    private Label _toastLabel = default!;
    private HBoxContainer _skillBar = default!;
    private Label _primaryLabel = default!;
    private Label _secondaryLabel = default!;
    private Label _skill1Label = default!;
    private Label _skill2Label = default!;
    private Label _skill3Label = default!;
    private Label _skill4Label = default!;
    private InventoryPanel _inventoryPanel = default!;

    private PlayerController? _player;

    public override void _Ready()
    {
        _staminaLabel = GetNode<Label>("Margin/VBox/StaminaLabel");
        _manaLabel = GetNode<Label>("Margin/VBox/ManaLabel");
        _goldLabel = GetNode<Label>("Margin/VBox/GoldLabel");
        _skillBar = GetNode<HBoxContainer>("Margin/VBox/SkillBar");
        _primaryLabel = GetNode<Label>("Margin/VBox/SkillBar/PrimaryLabel");
        _secondaryLabel = GetNode<Label>("Margin/VBox/SkillBar/SecondaryLabel");
        _skill1Label = GetNode<Label>("Margin/VBox/SkillBar/Skill1Label");
        _skill2Label = GetNode<Label>("Margin/VBox/SkillBar/Skill2Label");
        _skill3Label = GetNode<Label>("Margin/VBox/SkillBar/Skill3Label");
        _skill4Label = GetNode<Label>("Margin/VBox/SkillBar/Skill4Label");
        _toastLabel = GetNode<Label>("Margin/VBox/ToastLabel");
        _inventoryPanel = GetNode<InventoryPanel>("Margin/VBox/InventoryPanel");
    }

    public override void _Process(double delta)
    {
        ResolvePlayer();
        UpdateHudText();
        UpdateSkillBar();
        UpdateToast();

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
            _manaLabel.Text = "法力: --/--";
            _goldLabel.Text = "金币: 0";
            return;
        }

        var stamina = _player.ActorContext.Stamina;
        var mana = _player.ActorContext.Mana;
        _staminaLabel.Text = $"体力: {stamina.Current:0}/{stamina.Max:0}";
        _manaLabel.Text = $"法力: {mana.Current:0}/{mana.Max:0}";
        _goldLabel.Text = $"金币: {_player.Gold}";
    }

    private void UpdateSkillBar()
    {
        if (_player == null)
        {
            _skillBar.Visible = false;
            return;
        }

        _skillBar.Visible = _player.Features.EnableSkills;
        if (!_skillBar.Visible)
        {
            return;
        }

        UpdateSlotLabel(_primaryLabel, SkillSlot.Primary, "P");
        UpdateSlotLabel(_secondaryLabel, SkillSlot.Secondary, "S");
        UpdateSlotLabel(_skill1Label, SkillSlot.Skill1, "1");
        UpdateSlotLabel(_skill2Label, SkillSlot.Skill2, "2");
        UpdateSlotLabel(_skill3Label, SkillSlot.Skill3, "3");
        UpdateSlotLabel(_skill4Label, SkillSlot.Skill4, "4");

        // Secondary 瞄准中高亮
        if (_player.ActorContext.IsTargeting)
        {
            _secondaryLabel.Modulate = Colors.Yellow;
        }
    }

    private void UpdateSlotLabel(Label label, SkillSlot slot, string shortName)
    {
        var cooldown = _player!.ActorContext.Cooldowns.GetRemaining(slot);
        var def = SkillDatabase.DefaultBySlot[slot];
        var manaOk = _player.ActorContext.Mana.Current >= def.ManaCost;
        var ready = cooldown <= 0f;

        label.Text = ready ? shortName : $"{shortName}:{cooldown:0.0}";
        label.Modulate = ready && manaOk ? Colors.White : Colors.Gray;
    }

    private void UpdateToast()
    {
        if (_player == null)
        {
            _toastLabel.Visible = false;
            return;
        }

        var msg = _player.SkillToastMessage;
        _toastLabel.Visible = !string.IsNullOrEmpty(msg);
        if (_toastLabel.Visible)
        {
            _toastLabel.Text = msg;
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
