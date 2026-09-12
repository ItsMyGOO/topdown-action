using Godot;

namespace GodotGameTemplate.Game.Scenes.Enemies;

/// <summary>
/// 漂浮伤害数字：生成后上浮并淡出，动画结束自毁。
/// </summary>
public partial class DamageNumber : Label
{
    private const float RiseDistance = 24f;
    private const float LifetimeSeconds = 0.6f;

    /// <summary>
    /// 在容器内生成一条伤害数字并启动动画。
    /// </summary>
    public static DamageNumber Spawn(Node parent, Vector2 position, int damage)
    {
        var number = new DamageNumber { Text = damage.ToString() };
        number.ZIndex = 10;
        parent.AddChild(number);
        number.GlobalPosition = position + new Vector2((float)GD.RandRange(-6f, 6f), -18f);
        number.PlayAnimation();
        return number;
    }

    private void PlayAnimation()
    {
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(
            this,
            "position",
            Position + new Vector2(0f, -RiseDistance),
            LifetimeSeconds
        );
        tween.TweenProperty(this, "modulate:a", 0f, LifetimeSeconds);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
