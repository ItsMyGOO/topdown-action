using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotGameTemplate.Gameplay.Player;
using GodotGameTemplate.Gameplay.Progression.Paragon;

namespace GodotGameTemplate.Game.UI.Paragon;

/// <summary>
/// Paragon 面板（简化版）：四系线性加成，等级 ≥ 10 后每级 1 点。
/// </summary>
public partial class ParagonPanel : PanelContainer
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
            _rows.RemoveChild(child);
            child.QueueFree();
        }

        if (_player == null)
        {
            _rows.AddChild(new Label { Text = "未找到玩家" });
            return;
        }

        var level = _player.Leveling?.Level ?? 1;
        var paragon = _player.SessionParagon;

        _pointsLabel.Text =
            $"等级 {level}  可用点数: {paragon.AvailablePoints(level)}"
            + (
                level < ParagonModel.UnlockLevel
                    ? $"（{ParagonModel.UnlockLevel} 级解锁）"
                    : string.Empty
            );

        foreach (
            var (category, name, description) in new[]
            {
                (ParagonCategory.Brutality, "暴虐", "+1 伤害"),
                (ParagonCategory.Vitality, "活力", "+3 最大生命"),
                (ParagonCategory.Cunning, "智谋", "+1% 经验"),
                (ParagonCategory.Alacrity, "迅捷", "+0.5% 移速"),
            }
        )
        {
            var rank = paragon.Ranks.GetValueOrDefault(category);
            var captured = category;

            var row = new HBoxContainer();
            var label = new Label
            {
                Text = $"{name}  {rank}  {description}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            row.AddChild(label);

            var plusButton = new Button { Text = "+" };
            plusButton.Disabled = !paragon.Allocate(category, level); // 试探禁用态。
            row.AddChild(plusButton);

            _rows.AddChild(row);

            plusButton.Pressed += () => OnAllocatePressed(captured);
        }
    }

    private void OnAllocatePressed(ParagonCategory category)
    {
        if (_player == null)
        {
            return;
        }

        var level = _player.Leveling?.Level ?? 1;
        _player.SessionParagon.Allocate(category, level);
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
