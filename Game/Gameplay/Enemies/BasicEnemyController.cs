using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;
using GodotGameTemplate.Gameplay.Combat.Targeting;
using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Gameplay.Enemies;

/// <summary>
/// 最小敌人控制器：
/// 支持被点选、被命中，并在生命值归零时死亡。
/// 可选地应用精英/Boss 强化计划（<see cref="ApplyPlan"/>）。
/// </summary>
public partial class BasicEnemyController : CharacterBody2D, IHitReceiver, ITargetable
{
    [Signal]
    public delegate void DiedEventHandler(Vector2 pos, int xpReward);

    [Export]
    public int MaxHp { get; set; } = 3;

    [Export]
    public int XpReward { get; set; } = 5;

    [Export]
    public bool IsBoss { get; set; }

    /// <summary>
    /// 已应用的精英/Boss 强化计划（普通怪为 null）。
    /// </summary>
    public ElitePlan? Plan { get; private set; }

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
            RestoreBodyColor();
        }
    }

    /// <summary>
    /// 应用精英/Boss 强化计划：倍率写进属性，词缀与 Boss 做表现区分。
    /// 必须在 <see cref="_Ready"/> 之后调用（世界根生成期）。
    /// </summary>
    public void ApplyPlan(ElitePlan plan)
    {
        Plan = plan;

        MaxHp = Mathf.Max(1, Mathf.RoundToInt(MaxHp * plan.HpMultiplier));
        Hp = MaxHp;
        XpReward = Mathf.Max(1, Mathf.RoundToInt(XpReward * plan.XpMultiplier));

        if (Body == null)
        {
            return;
        }

        if (plan.IsBoss)
        {
            Body.Scale *= 1.8f;
        }

        RestoreBodyColor();
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

        EmitSignal(SignalName.Died, GlobalPosition, XpReward);
        RemoveFromGroup("targetable");
        QueueFree();
    }

    private void RestoreBodyColor()
    {
        if (Body == null)
        {
            return;
        }

        Body.Color = Plan switch
        {
            { IsBoss: true } => new Color(0.62f, 0.12f, 0.12f),
            { Affix: EliteAffix.Sturdy } => new Color(0.95f, 0.55f, 0.15f),
            { Affix: EliteAffix.Cunning } => new Color(0.95f, 0.85f, 0.25f),
            { Affix: EliteAffix.Rich } => new Color(0.98f, 0.75f, 0.4f),
            _ => AliveColor,
        };
    }
}
