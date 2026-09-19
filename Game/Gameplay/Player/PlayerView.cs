using Godot;
using GodotGameTemplate.Gameplay.Actors;

namespace GodotGameTemplate.Gameplay.Player;

/// <summary>
/// 玩家表现层：AnimatedSprite2D（SpriteFrames 资源，编辑器可视化可调）。
/// <para>
/// 动画命名规范：{动作}_{方向}（如 idle_e / run_sw），方向 8 向（E 起顺时针）。
/// 一次性动作（melee/cast/hurt/roll）播放期间锁定，播完自动回 idle/run。
/// </para>
/// </summary>
public partial class PlayerView : Node2D
{
    private AnimatedSprite2D _sprite = default!;
    private bool _oncePlaying;
    private string _direction = "s";

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        _sprite.AnimationFinished += OnAnimationFinished;
    }

    public void Sync(ActorContext context, float delta)
    {
        // 一次性动画播放期间锁定。
        if (_oncePlaying)
        {
            return;
        }

        var moving = context.Velocity.LengthSquared() > 1f;
        Play(moving ? "run" : "idle");
    }

    /// <summary>
    /// 播放一次性动作（普攻/施法/受击）；由对应状态在进入时调用。
    /// </summary>
    public void PlayOnce(string action)
    {
        _oncePlaying = true;
        Play(action);
    }

    /// <summary>
    /// 更新朝向后缀（e/se/s/sw/w/nw/n/ne）；由控制器在朝向变化时调用。
    /// </summary>
    public void SetDirection(string suffix)
    {
        _direction = suffix;
    }

    private void Play(string action)
    {
        var animName = $"{action}_{_direction}";

        if (_sprite.Animation != animName || !_sprite.IsPlaying())
        {
            _sprite.Play(animName);
        }
    }

    private void OnAnimationFinished()
    {
        _oncePlaying = false;
    }
}
