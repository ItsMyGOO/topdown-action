using System.Collections.Generic;
using Godot;
using GodotGameTemplate.Game.UI.Inventory;
using GodotGameTemplate.Game.UI.Stash;
using GodotGameTemplate.Game.UI.Talents;
using GodotGameTemplate.Gameplay.Player;
using GodotGameTemplate.Gameplay.Progression.Tiers;
using GodotGameTemplate.Gameplay.Skills;
using SkillSlot = GodotGameTemplate.Gameplay.Input.Commands.SkillSlot;

namespace GodotGameTemplate.Game.UI.Hud;

/// <summary>
/// D4 风格 HUD：底部技能栏（悬停显示技能说明）+ 左右生命/法力条 + 底部经验条。
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _statsLine = default!;
    private Label _resourcesLine = default!;
    private Label _healthLabel = default!;
    private ProgressBar _healthBar = default!;
    private Label _manaLabel = default!;
    private ProgressBar _manaBar = default!;
    private ProgressBar _xpTrack = default!;
    private HBoxContainer _skillBar = default!;
    private Label _toastLabel = default!;
    private HBoxContainer _touchTargetingControls = default!;
    private Button _confirmButton = default!;
    private Button _cancelButton = default!;
    private InventoryPanel _inventoryPanel = default!;
    private TalentPanel _talentPanel = default!;
    private StashPanel _stashPanel = default!;
    private GodotGameTemplate.Game.UI.Paragon.ParagonPanel _paragonPanel = default!;

    private PanelContainer _tooltip = default!;
    private Label _tooltipName = default!;
    private Label _tooltipMeta = default!;
    private Label _tooltipDesc = default!;

    private readonly Dictionary<SkillSlot, Button> _slotButtons = new();
    private readonly Dictionary<SkillSlot, Label> _slotCooldownLabels = new();

    private PlayerController? _player;

    private static readonly (SkillSlot Slot, string Key)[] SlotKeys =
    [
        (SkillSlot.Primary, "左键"),
        (SkillSlot.Secondary, "右键"),
        (SkillSlot.Skill1, "1"),
        (SkillSlot.Skill2, "2"),
        (SkillSlot.Skill3, "3"),
        (SkillSlot.Skill4, "4"),
    ];

    public override void _Ready()
    {
        _statsLine = GetNode<Label>("TopLeft/VBox/StatsLine");
        _resourcesLine = GetNode<Label>("TopLeft/VBox/ResourcesLine");
        _healthLabel = GetNode<Label>("BottomBar/HealthBox/HealthLabel");
        _healthBar = GetNode<ProgressBar>("BottomBar/HealthBox/HealthBar");
        _manaLabel = GetNode<Label>("BottomBar/ManaBox/ManaLabel");
        _manaBar = GetNode<ProgressBar>("BottomBar/ManaBox/ManaBar");
        _xpTrack = GetNode<ProgressBar>("XpTrack");
        _skillBar = GetNode<HBoxContainer>("BottomBar/SkillBar");
        _toastLabel = GetNode<Label>("ToastLabel");
        _touchTargetingControls = GetNode<HBoxContainer>("TouchTargetingControls");
        _confirmButton = GetNode<Button>("TouchTargetingControls/ConfirmButton");
        _cancelButton = GetNode<Button>("TouchTargetingControls/CancelButton");
        _inventoryPanel = GetNode<InventoryPanel>("InventoryPanel");
        _talentPanel = GetNode<TalentPanel>("TalentPanel");
        _stashPanel = GetNode<StashPanel>("StashPanel");
        _paragonPanel = GetNode<GodotGameTemplate.Game.UI.Paragon.ParagonPanel>("ParagonPanel");
        _tooltip = GetNode<PanelContainer>("SkillTooltip");
        _tooltipName = GetNode<Label>("SkillTooltip/Margin/VBox/NameLabel");
        _tooltipMeta = GetNode<Label>("SkillTooltip/Margin/VBox/MetaLabel");
        _tooltipDesc = GetNode<Label>("SkillTooltip/Margin/VBox/DescLabel");

        GetNode<Label>("InteractPrompt").AddToGroup("interact_prompt");

        BuildSkillSlots();

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

        if (Input.IsActionJustPressed("open_paragon"))
        {
            _paragonPanel.Visible = !_paragonPanel.Visible;
            if (_paragonPanel.Visible)
            {
                _paragonPanel.Refresh();
            }
        }
    }

    /// <summary>
    /// 代码构建 6 个技能槽按钮（键位 + 冷却数字），悬停显示技能说明。
    /// </summary>
    private void BuildSkillSlots()
    {
        foreach (var (slot, key) in SlotKeys)
        {
            var captured = slot;

            var box = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };

            var button = new Button { CustomMinimumSize = new Vector2(46f, 46f), Text = key };
            button.MouseEntered += () => ShowTooltip(captured);
            button.MouseExited += HideTooltip;

            var cooldown = new Label
            {
                Text = string.Empty,
                HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

            box.AddChild(button);
            box.AddChild(cooldown);
            _skillBar.AddChild(box);

            _slotButtons[slot] = button;
            _slotCooldownLabels[slot] = cooldown;
        }
    }

    private void ShowTooltip(SkillSlot slot)
    {
        if (_player == null)
        {
            return;
        }

        var def = SkillDatabase.SkillsFor(_player.SessionClassId)[slot];
        var names = new Dictionary<SkillSlot, string>
        {
            [SkillSlot.Primary] = "基础攻击",
            [SkillSlot.Secondary] = "核心技能",
            [SkillSlot.Skill1] = "技能 1",
            [SkillSlot.Skill2] = "技能 2",
            [SkillSlot.Skill3] = "技能 3",
            [SkillSlot.Skill4] = "技能 4",
        };

        _tooltipName.Text = names[slot];
        _tooltipMeta.Text =
            $"消耗 {def.ManaCost:0} 法力"
            + (def.CooldownSeconds > 0f ? $"  冷却 {def.CooldownSeconds:0.#}s" : "")
            + (def.Range > 0f ? $"  射程 {def.Range:0}" : "");
        _tooltipDesc.Text = string.IsNullOrEmpty(def.Description) ? "（无描述）" : def.Description;

        _tooltip.Visible = true;
        var barRect = _skillBar.GetGlobalRect();
        var size = _tooltip.GetCombinedMinimumSize();
        _tooltip.GlobalPosition = new Vector2(barRect.Position.X, barRect.Position.Y - size.Y - 8f);
    }

    private void HideTooltip()
    {
        _tooltip.Visible = false;
    }

    private void UpdateHudText()
    {
        if (_player == null)
        {
            _statsLine.Text = "未找到角色";
            _resourcesLine.Text = string.Empty;
            return;
        }

        var leveling = _player.Leveling;
        var tier = WorldTierDatabase.Get(_player.SessionWorldTier);
        _statsLine.Text =
            $"{_player.Class.Name}  Lv.{leveling?.Level ?? 1}"
            + $"  {tier.Name}难度  金币 {_player.Gold}";

        var health = _player.ActorContext.Health;
        var mana = _player.ActorContext.Mana;
        var evade = _player.ActorContext.Evade;
        var potions = _player.Potions;
        _resourcesLine.Text =
            $"闪避 {evade.Available}/{evade.MaxCharges}  药水 {potions.Available}/{potions.MaxCharges}(Q)"
            + "  [I]背包 [T]天赋 [P]巅峰";

        _healthLabel.Text = $"生命 {health.Current:0}/{health.Max:0}";
        _healthBar.MaxValue = Mathf.Max(1f, health.Max);
        _healthBar.Value = health.Current;

        _manaLabel.Text = $"法力 {mana.Current:0}/{mana.Max:0}";
        _manaBar.MaxValue = Mathf.Max(1f, mana.Max);
        _manaBar.Value = mana.Current;

        if (leveling != null)
        {
            _xpTrack.MaxValue = Mathf.Max(1, leveling.XpToNextLevel);
            _xpTrack.Value = leveling.CurrentXp;
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

        foreach (var (slot, _) in SlotKeys)
        {
            if (!_slotButtons.TryGetValue(slot, out var button))
            {
                continue;
            }

            var cooldown = _player.ActorContext.Cooldowns.GetRemaining(slot);
            var def = SkillDatabase.SkillsFor(_player.SessionClassId)[slot];
            var manaOk = _player.ActorContext.Mana.Current >= def.ManaCost;

            _slotCooldownLabels[slot].Text = cooldown > 0f ? $"{cooldown:0.0}" : string.Empty;
            button.Modulate = cooldown <= 0f && manaOk ? Colors.White : Colors.Gray;
        }

        if (
            _player.ActorContext.IsTargeting
            && _slotButtons.TryGetValue(SkillSlot.Secondary, out var secondary)
        )
        {
            secondary.Modulate = Colors.Yellow;
        }
    }

    private void UpdateTouchTargetingControls()
    {
        if (_player == null)
        {
            _touchTargetingControls.Visible = false;
            return;
        }

        var isTouchscreen = DisplayServer.IsTouchscreenAvailable();
        _touchTargetingControls.Visible =
            isTouchscreen && _player.Features.EnableSkills && _player.ActorContext.IsTargeting;

        if (!_touchTargetingControls.Visible)
        {
            _player.TouchInput.SetConfirmPressed(false);
            _player.TouchInput.SetCancelPressed(false);
        }
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
