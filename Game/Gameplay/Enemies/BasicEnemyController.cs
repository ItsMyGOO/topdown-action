using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;
using GodotGameTemplate.Gameplay.Combat.Targeting;

namespace GodotGameTemplate.Gameplay.Enemies;

/// <summary>
/// 最小敌人控制器：
/// 支持被点选、被命中，并在生命值归零时死亡。
/// </summary>
public partial class BasicEnemyController : CharacterBody2D, IHitReceiver, ITargetable
{
    [Export]
    public int MaxHp { get; set; } = 3;

    [Export]
    public Polygon2D? Body { get; set; }

    [Export]
    public Color AliveColor { get; set; } = new(0.91f, 0.35f, 0.35f, 1f);

    [Export]
    public Color HitFlashColor { get; set; } = Colors.White;

    [Export]
    public float HitFlashDuration { get; set; } = 0.08f;

    private double _hitFlashRemaining;

    /// <summary>
    /// 当前生命值。
    /// </summary>
    public int Hp { get; private set; }

    /// <summary>
    /// 暴露给纯逻辑系统使用的实例 Id。
    /// </summary>
    public ulong InstanceId => GetInstanceId();

    public override void _Ready()
    {
        Hp = MaxHp;
        Body ??= GetNodeOrNull<Polygon2D>("Body");
        AddToGroup("targetable");

        if (Body != null)
        {
            Body.Color = AliveColor;
        }
    }

    public override void _Process(double delta)
    {
        if (_hitFlashRemaining <= 0d || Body == null)
        {
            return;
        }

        _hitFlashRemaining -= delta;
        if (_hitFlashRemaining <= 0d)
        {
            Body.Color = AliveColor;
        }
    }

    public void ReceiveHit(HitContext hit)
    {
        Hp = Mathf.Max(0, Hp - 1);
        _hitFlashRemaining = HitFlashDuration;

        if (Body != null)
        {
            Body.Color = HitFlashColor;
        }

        if (Hp > 0)
        {
            return;
        }

        RemoveFromGroup("targetable");
        QueueFree();
    }
}
