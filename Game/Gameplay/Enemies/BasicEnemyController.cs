using Godot;
using GodotGameTemplate.Game.Scenes.Enemies;
using GodotGameTemplate.Gameplay.Actors.Combat;
using GodotGameTemplate.Gameplay.Combat;
using GodotGameTemplate.Gameplay.Combat.Targeting;
using GodotGameTemplate.Gameplay.Enemies.Feedback;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Player;

namespace GodotGameTemplate.Gameplay.Enemies;

/// <summary>
/// 最小敌人控制器：
/// 支持被点选、被命中（血条/伤害数字/击退反馈），AI 追击，生命归零时播放死亡动画。
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
    /// 触碰玩家的伤害（受玩家护甲减免）。
    /// </summary>
    [Export]
    public int Damage { get; set; } = 8;

    /// <summary>
    /// 减免玩家造成的固定伤害（抗性，保底 1 点）。
    /// </summary>
    [Export]
    public int ResistFlat { get; set; } = 0;

    /// <summary>
    /// 移动速度（像素/秒）。
    /// </summary>
    [Export]
    public float Speed { get; set; } = 60f;

    /// <summary>
    /// 进入追击的警戒半径。
    /// </summary>
    [Export]
    public float AggroRange { get; set; } = 140f;

    /// <summary>
    /// 距出生点超过此距离强制脱战回家。
    /// </summary>
    [Export]
    public float LeashRange { get; set; } = 260f;

    [Export]
    public Polygon2D? Body { get; set; }

    [Export]
    public EnemyHealthBar? HealthBar { get; set; }

    [Export]
    public Color AliveColor { get; set; } = new(0.91f, 0.35f, 0.35f, 1f);

    [Export]
    public Color HitFlashColor { get; set; } = Colors.White;

    [Export]
    public float HitFlashDuration { get; set; } = 0.08f;

    private const float TouchRange = 26f;
    private const double TouchCooldownSeconds = 0.8d;
    private const float KnockbackImpulse = 90f;
    private const float BossKnockbackScale = 0.3f;
    private const float DeathSeconds = 0.18f;

    private readonly EnemyAiOrchestrator _ai = new();
    private readonly KnockbackModel _knockback = new();
    private Vector2 _homePosition;
    private double _hitFlashRemaining;
    private double _touchCooldownRemaining;
    private bool _dying;

    /// <summary>
    /// 当前生命值。
    /// </summary>
    public int Hp { get; private set; }

    /// <summary>
    /// 暴露给纯逻辑系统使用的实例 Id。
    /// </summary>
    public ulong InstanceId => GetInstanceId();

    /// <summary>
    /// 已应用的精英/Boss 强化计划（普通怪为 null）。
    /// </summary>
    public ElitePlan? Plan { get; private set; }

    public override void _Ready()
    {
        Hp = MaxHp;
        _homePosition = GlobalPosition;
        Body ??= GetNodeOrNull<Polygon2D>("Body");
        HealthBar ??= GetNodeOrNull<EnemyHealthBar>("HealthBar");
        AddToGroup("targetable");

        if (Body != null)
        {
            Body.Color = AliveColor;
        }

        HealthBar?.Update(Hp, MaxHp);
    }

    public override void _Process(double delta)
    {
        if (_dying || _hitFlashRemaining <= 0d || Body == null)
        {
            return;
        }

        _hitFlashRemaining -= delta;
        if (_hitFlashRemaining <= 0d)
        {
            RestoreBodyColor();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_dying)
        {
            return;
        }

        _touchCooldownRemaining = Mathf.Max(0d, _touchCooldownRemaining - delta);
        _knockback.Tick((float)delta);

        if (GetTree().GetFirstNodeInGroup("player") is not PlayerController player)
        {
            return;
        }

        if (
            _touchCooldownRemaining <= 0d
            && GlobalPosition.DistanceTo(player.GlobalPosition) <= TouchRange
        )
        {
            player.TakeDamage(Damage);
            _touchCooldownRemaining = TouchCooldownSeconds;
        }

        RunAi(player);
    }

    private void RunAi(PlayerController player)
    {
        var decision = _ai.Evaluate(
            new EnemyAiSnapshot(
                GlobalPosition,
                _homePosition,
                player.GlobalPosition,
                AggroRange,
                AggroRange * 1.25f,
                LeashRange,
                !player.ActorContext.Health.IsEmpty
            )
        );

        if (decision.ReachedHome)
        {
            Hp = MaxHp;
            UpdateHealthBar();
        }

        var velocity =
            decision.State == EnemyAiState.Idle ? Vector2.Zero : decision.Direction * Speed;
        velocity += new Vector2(_knockback.X, _knockback.Y);

        if (velocity == Vector2.Zero)
        {
            return;
        }

        Velocity = velocity;
        MoveAndSlide();
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
        Damage = Mathf.Max(1, Mathf.RoundToInt(Damage * plan.DamageMultiplier));
        Speed = Mathf.Max(0f, Speed * plan.SpeedMultiplier);

        if (plan.IsBoss)
        {
            if (Body != null)
            {
                Body.Scale *= 1.8f;
            }

            HealthBar?.SetWidth(44f);
        }

        if (Body != null)
        {
            Body.Color = AliveColor;
        }

        RestoreBodyColor();
        UpdateHealthBar();
    }

    public void ReceiveHit(HitContext hit)
    {
        if (_dying)
        {
            return;
        }

        var effective = DamageMath.Apply(hit.Damage, ResistFlat);
        Hp = Mathf.Max(0, Hp - effective);
        _hitFlashRemaining = HitFlashDuration;

        if (Body != null)
        {
            Body.Color = HitFlashColor;
        }

        UpdateHealthBar();
        SpawnDamageNumber(effective);
        ApplyKnockback(hit.Direction);

        if (Hp > 0)
        {
            return;
        }

        Die();
    }

    private void UpdateHealthBar()
    {
        HealthBar?.Update(Hp, MaxHp);
    }

    private void SpawnDamageNumber(int effectiveDamage)
    {
        if (GetParent() is Node parent)
        {
            DamageNumber.Spawn(parent, GlobalPosition, effectiveDamage);
        }
    }

    private void ApplyKnockback(Vector2 hitDirection)
    {
        if (hitDirection == Vector2.Zero)
        {
            return;
        }

        var impulse = KnockbackImpulse * (IsBoss ? BossKnockbackScale : 1f);
        var direction = hitDirection.Normalized();
        _knockback.Apply(direction.X, direction.Y, impulse);
    }

    /// <summary>
    /// 死亡：立即结算（信号、组移除、关碰撞），表现层播放缩放渐隐动画后释放节点。
    /// </summary>
    private void Die()
    {
        _dying = true;
        EmitSignal(SignalName.Died, GlobalPosition, XpReward);
        RemoveFromGroup("targetable");

        if (HealthBar != null)
        {
            HealthBar.Visible = false;
        }

        GetNodeOrNull<CollisionShape2D>("CollisionShape2D")?.SetDeferred("disabled", true);

        if (Body == null)
        {
            QueueFree();
            return;
        }

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(Body, "scale", Vector2.Zero, DeathSeconds);
        tween.TweenProperty(Body, "modulate:a", 0f, DeathSeconds);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
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
