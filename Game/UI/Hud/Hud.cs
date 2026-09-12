using Godot;
using GodotGameTemplate.Game.UI.Inventory;
using GodotGameTemplate.Game.UI.Talents;
using GodotGameTemplate.Gameplay.Player;
using GodotGameTemplate.Gameplay.Skills;
using SkillSlot = GodotGameTemplate.Gameplay.Input.Commands.SkillSlot;

namespace GodotGameTemplate.Game.UI.Hud;

/// <summary>
/// 最小 HUD：显示体力与金币占位，并监听 <c>open_inventory</c> 切换背包面板。
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _lifeLabel = default!;
    private Label _evadeLabel = default!;
    private Label _potionLabel = default!;
    private Label _manaLabel = default!;
    private Label _goldLabel = default!;
    private Label _levelLabel = default!;
    private Label _toastLabel = default!;
    private HBoxContainer _skillBar = default!;
    private HBoxContainer _touchTargetingControls = default!;
    private Button _confirmButton = default!;
    private Button _cancelButton = default!;
    private Label _primaryLabel = default!;
    private Label _secondaryLabel = default!;
    private Label _skill1Label = default!;
    private Label _skill2Label = default!;
    private Label _skill3Label = default!;
    private Label _skill4Label = default!;
    private InventoryPanel _inventoryPanel = default!;
    private TalentPanel _talentPanel = default!;

    private PlayerController? _player;

    public override void _Ready()
    {
        _lifeLabel = GetNode<Label>("Margin/VBox/LifeLabel");
        _evadeLabel = GetNode<Label>("Margin/VBox/EvadeLabel");
        _potionLabel = GetNode<Label>("Margin/VBox/PotionLabel");
        _manaLabel = GetNode<Label>("Margin/VBox/ManaLabel");
        _goldLabel = GetNode<Label>("Margin/VBox/GoldLabel");
        _levelLabel = GetNode<Label>("Margin/VBox/LevelLabel");
        _skillBar = GetNode<HBoxContainer>("Margin/VBox/SkillBar");
        _touchTargetingControls = GetNode<HBoxContainer>("Margin/VBox/TouchTargetingControls");
        _confirmButton = GetNode<Button>("Margin/VBox/TouchTargetingControls/ConfirmButton");
        _cancelButton = GetNode<Button>("Margin/VBox/TouchTargetingControls/CancelButton");
        _primaryLabel = GetNode<Label>("Margin/VBox/SkillBar/PrimaryLabel");
        _secondaryLabel = GetNode<Label>("Margin/VBox/SkillBar/SecondaryLabel");
        _skill1Label = GetNode<Label>("Margin/VBox/SkillBar/Skill1Label");
        _skill2Label = GetNode<Label>("Margin/VBox/SkillBar/Skill2Label");
        _skill3Label = GetNode<Label>("Margin/VBox/SkillBar/Skill3Label");
        _skill4Label = GetNode<Label>("Margin/VBox/SkillBar/Skill4Label");
        _toastLabel = GetNode<Label>("Margin/VBox/ToastLabel");
        _inventoryPanel = GetNode<InventoryPanel>("Margin/VBox/InventoryPanel");
        _talentPanel = GetNode<TalentPanel>("Margin/VBox/TalentPanel");

        // 触屏最小可用：Secondary 选点时提供确认/取消按钮。
        _confirmButton.ButtonDown += () => _player?.TouchInput.SetConfirmPressed(true);
        _confirmButton.ButtonUp += () => _player?.TouchInput.SetConfirmPressed(false);
        _cancelButton.ButtonDown += () => _player?.TouchInput.SetCancelPressed(true);
        _cancelButton.ButtonUp += () => _player?.TouchInput.SetCancelPressed(false);
    }

    public override void _Process(double delta)
    {
        ResolvePlayer();
        UpdateHudText();
        UpdateSkillBar();
        UpdateTouchTargetingControls();
        UpdateToast();

        if (Input.IsActionJustPressed("open_inventory"))
        {
            _inventoryPanel.Visible = !_inventoryPanel.Visible;
            if (_inventoryPanel.Visible)
            {
                _inventoryPanel.Refresh();
            }
        }

        if (Input.IsActionJustPressed("open_talents"))
        {
            _talentPanel.Visible = !_talentPanel.Visible;
            if (_talentPanel.Visible)
            {
                _talentPanel.Refresh();
            }
        }
    }

    private void UpdateHudText()
    {
        if (_player == null)
        {
            _lifeLabel.Text = "生命: --/--";
            _evadeLabel.Text = "闪避: --";
            _potionLabel.Text = "药水: --";
            _manaLabel.Text = "法力: --/--";
            _goldLabel.Text = "金币: 0";
            _levelLabel.Visible = false;
            return;
        }

        var health = _player.ActorContext.Health;
        var evade = _player.ActorContext.Evade;
        var mana = _player.ActorContext.Mana;
        _lifeLabel.Text = $"生命: {health.Current:0}/{health.Max:0}";
        _evadeLabel.Text = $"闪避: {evade.Available}/{evade.MaxCharges}";
        var potions = _player.Potions;
        _potionLabel.Text = $"药水: {potions.Available}/{potions.MaxCharges} (Q)";
        _manaLabel.Text = $"法力: {mana.Current:0}/{mana.Max:0}";
        _goldLabel.Text = $"金币: {_player.Gold}";

        var leveling = _player.Leveling;
        _levelLabel.Visible = leveling != null;
        if (leveling != null)
        {
            _levelLabel.Text =
                $"等级: {leveling.Level}  经验: {leveling.CurrentXp}/{leveling.XpToNextLevel}";
        }
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

    private void UpdateTouchTargetingControls()
    {
        if (_player == null)
        {
            _touchTargetingControls.Visible = false;
            return;
        }

        // 仅在触屏设备上显示（鼠标/手柄仍走原交互）。
        var isTouchscreen = DisplayServer.IsTouchscreenAvailable();
        _touchTargetingControls.Visible =
            isTouchscreen && _player.Features.EnableSkills && _player.ActorContext.IsTargeting;

        if (!_touchTargetingControls.Visible)
        {
            // 避免按钮“卡住”导致边沿检测失效。
            _player.TouchInput.SetConfirmPressed(false);
            _player.TouchInput.SetCancelPressed(false);
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
