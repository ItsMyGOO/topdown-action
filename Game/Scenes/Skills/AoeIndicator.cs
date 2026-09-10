using Godot;

namespace GodotGameTemplate.Game.Scenes.Skills;

/// <summary>
/// 地面范围指示器（用于技能选点）。
/// </summary>
public partial class AoeIndicator : Node2D
{
    [Export]
    public float Radius { get; set; } = 24f;

    public override void _Draw()
    {
        DrawArc(
            Vector2.Zero,
            Radius,
            startAngle: 0f,
            endAngle: Mathf.Tau,
            pointCount: 48,
            color: new Color(0.2f, 0.9f, 0.9f, 0.85f),
            width: 2f,
            antialiased: true
        );
        DrawCircle(Vector2.Zero, 2f, new Color(0.2f, 0.9f, 0.9f, 0.9f));
    }

    public void SetRadius(float radius)
    {
        Radius = radius;
        QueueRedraw();
    }
}
