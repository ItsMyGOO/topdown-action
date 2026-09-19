using Godot;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.Pixel;

namespace GodotGameTemplate.Gameplay.Player;

/// <summary>
/// 玩家表现层：骑士序列帧动画（Idle/Run 循环；Melee/Cast 由攻击/施放状态驱动）。
/// 切表时同步 hframes/vframes，帧坐标按行优先推进。
/// </summary>
public partial class PlayerView : Node2D
{
    private Sprite2D _sprite = default!;
    private float _clock;
    private PlayerAnim _current = PlayerAnim.Idle;
    private AnimDefinition _definition = AnimationCatalog.Get(PlayerAnim.Idle);

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        ApplySheet(AnimationCatalog.Get(PlayerAnim.Idle));
    }

    public void Sync(ActorContext context, float delta)
    {
        _clock += delta;

        var moving = context.Velocity.LengthSquared() > 1f;
        Play(moving ? PlayerAnim.Run : PlayerAnim.Idle);

        var frame = AnimationCatalog.FrameFor(_clock, _definition);
        _sprite.FrameCoords = new Vector2I(
            frame % _definition.Columns,
            frame / _definition.Columns
        );
    }

    /// <summary>
    /// 播放一次性动作（如普攻/施法）；由对应状态在进入时调用。
    /// </summary>
    public void PlayOnce(PlayerAnim anim)
    {
        Play(anim);
        _clock = 0f;
    }

    private void Play(PlayerAnim anim)
    {
        if (_current == anim)
        {
            return;
        }

        _current = anim;
        _clock = 0f;
        _definition = AnimationCatalog.Get(anim);
        ApplySheet(_definition);
    }

    private void ApplySheet(AnimDefinition definition)
    {
        _sprite.Texture = GD.Load<Texture2D>(definition.Sheet);
        _sprite.Hframes = definition.Columns;
        _sprite.Vframes = definition.Rows;
    }
}
