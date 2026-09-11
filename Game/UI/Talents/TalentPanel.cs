using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotGameTemplate.Gameplay.Player;
using GodotGameTemplate.Gameplay.Progression.Talents;

namespace GodotGameTemplate.Game.UI.Talents;

/// <summary>
/// 天赋面板：展示全部天赋与可用点数，提供 [+] 分配按钮（受点数/上限/前置约束）。
/// </summary>
public partial class TalentPanel : PanelContainer
{
    private Label _pointsLabel = default!;
    private VBoxContainer _rows = default!;
    private PlayerController? _player;

    public override void _Ready()
    {
        _pointsLabel = GetNode<Label>("Margin/VBox/PointsLabel");
        _rows = GetNode<VBoxContainer>("Margin/VBox/Scroll/Rows");
        Refresh();
    }

    public void Refresh()
    {
        ResolvePlayer();

        foreach (var child in _rows.GetChildren().OfType<Node>().ToArray())
        {
            // 立即摘除再延迟释放：避免同帧重复 Refresh 时旧行仍在树上造成重复。
            _rows.RemoveChild(child);
            child.QueueFree();
        }

        if (_player == null)
        {
            _rows.AddChild(new Label { Text = "未找到玩家（group: player）" });
            return;
        }

        var leveling = _player.Leveling;
        var level = leveling?.Level ?? 1;
        var talents = _player.SessionTalents;
        var ranks = talents.Ranks;
        var summary = TalentStats.Aggregate(talents);

        _pointsLabel.Text = $"等级 {level}  可用点数: {talents.AvailablePoints(level)}";
        _rows.AddChild(
            new Label
            {
                Text =
                    $"当前加成: 伤害+{summary.BonusDamage}  生命+{summary.BonusMaxHealth:0}  "
                    + $"法力+{summary.BonusMaxMana:0}  体力+{summary.BonusMaxStamina:0}  "
                    + $"经验×{summary.XpMultiplier:0.00}  移速×{summary.MoveSpeedMultiplier:0.00}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            }
        );

        foreach (var definition in TalentDatabase.Catalog)
        {
            var rank = ranks.GetValueOrDefault(definition.Id);
            var captured = definition.Id;

            var row = new HBoxContainer();

            var requirement =
                definition.RequiresId == null
                    ? null
                    : $"（需 {TalentDatabase.Get(definition.RequiresId)!.Name} {definition.RequiresRank} 级）";
            var label = new Label
            {
                Text =
                    $"{definition.Name}  {rank}/{definition.MaxRank}  {definition.Description}{requirement}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            row.AddChild(label);

            var plusButton = new Button { Text = "+" };
            plusButton.Disabled = !talents.CanAllocate(definition.Id, level);
            plusButton.Pressed += () => OnAllocatePressed(captured);
            row.AddChild(plusButton);

            _rows.AddChild(row);
        }
    }

    private void OnAllocatePressed(string talentId)
    {
        if (_player == null)
        {
            return;
        }

        var level = _player.Leveling?.Level ?? 1;
        _player.SessionTalents.Allocate(talentId, level);
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
