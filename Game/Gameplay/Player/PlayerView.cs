using Godot;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.Pixel;

namespace GodotGameTemplate.Gameplay.Player;

/// <summary>
/// 玩家表现层：像素序列帧动画（朝向行 + 行走帧循环）。
/// </summary>
public partial class PlayerView : Node2D
{
    private Sprite2D _sprite = default!;
    private float _clock;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
    }

    public void Sync(ActorContext context, float delta)
    {
        _clock += delta;

        var moving = context.Velocity.LengthSquared() > 1f;
        var anim = FacingAnimationResolver.Resolve(context.Facing, moving, _clock);

        _sprite.FrameCoords = new Vector2I(anim.Frame, anim.Row);
    }
}
