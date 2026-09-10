using Godot;
using GodotGameTemplate.Gameplay.Progression;
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Gameplay.Actors;

public sealed class ActorContext
{
    public ActorIntent Intent { get; set; } = ActorIntent.None;

    public Vector2 Velocity { get; set; } = Vector2.Zero;

    public Vector2 Facing { get; set; } = Vector2.Down;

    /// <summary>
    /// 体力模型（纯逻辑），用于翻滚/技能等消耗。
    /// </summary>
    public StaminaModel Stamina { get; } = new();

    /// <summary>
    /// 法力模型（纯逻辑），用于技能消耗。
    /// </summary>
    public ManaModel Mana { get; private set; } = new();

    /// <summary>
    /// 技能冷却模型（纯逻辑）。
    /// </summary>
    public CooldownModel Cooldowns { get; private set; } = new();

    public bool CanMove { get; set; } = true;

    public bool CanAttack { get; set; } = true;

    public bool IsAttacking { get; set; }

    public bool IsEvading { get; set; }

    public bool IsCasting { get; set; }

    public bool CastFinishedThisFrame { get; set; }

    public bool IsTargeting { get; set; }

    public bool TargetingFinishedThisFrame { get; set; }

    public Vector2 AttackFacing { get; set; } = Vector2.Down;

    public bool AttackFinishedThisFrame { get; set; }

    public bool EvadeFinishedThisFrame { get; set; }

    public bool HasMoveInput => CanMove && Intent.HasMoveInput;

    /// <summary>
    /// 绑定技能资源模型（用于跨场景共享，例如来自 <see cref="Gameplay.Session.GameSession"/>）。
    /// </summary>
    public void BindSkillResources(ManaModel mana, CooldownModel cooldowns)
    {
        Mana = mana;
        Cooldowns = cooldowns;
    }
}
