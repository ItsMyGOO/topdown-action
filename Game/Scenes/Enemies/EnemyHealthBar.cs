using Godot;
using GodotGameTemplate.Gameplay.Enemies.Feedback;

namespace GodotGameTemplate.Game.Scenes.Enemies;

/// <summary>
/// 敌人头顶世界空间血条：自绘深色底 + 红色填充，满血隐藏。
/// </summary>
public partial class EnemyHealthBar : Node2D
{
    private int _hp = 1;
    private int _maxHp = 1;
    private float _width = 28f;

    public override void _Draw()
    {
        if (!Visible)
        {
            return;
        }

        var backgroundSize = new Vector2(_width, 4f);
        DrawRect(new Rect2(-backgroundSize / 2f, backgroundSize), new Color(0f, 0f, 0f, 0.6f));

        if (_maxHp > 0)
        {
            var fillWidth = _width * _hp / _maxHp;
            DrawRect(
                new Rect2(new Vector2(-_width / 2f, -2f), new Vector2(fillWidth, 4f)),
                new Color(0.85f, 0.15f, 0.15f)
            );
        }
    }

    /// <summary>
    /// 更新血量并按规则显隐。
    /// </summary>
    public void Update(int hp, int maxHp)
    {
        _hp = hp;
        _maxHp = maxHp;
        Visible = EnemyFeedbackRules.ShowHealthBar(hp, maxHp);
        QueueRedraw();
    }

    /// <summary>
    /// 设置血条宽度（Boss 更宽）。
    /// </summary>
    public void SetWidth(float width)
    {
        _width = width;
        QueueRedraw();
    }
}
