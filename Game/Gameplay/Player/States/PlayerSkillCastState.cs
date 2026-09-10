using System;
using Godot;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;
using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Gameplay.Player.States;

/// <summary>
/// 技能施放状态（最小实现）：
/// <para>
/// - 进入时扣 Mana、开启 CD，并生成技能效果节点
/// - 施放期间禁移动（最短后摇），结束后切回 Idle/Move
/// </para>
/// </summary>
public sealed class PlayerSkillCastState : IState<ActorContext>
{
    private readonly Func<SkillSlot, SkillDefinition> _getSkill;
    private readonly Action<SkillDefinition, Vector2, Vector2?> _spawnEffect;

    private SkillSlot _slot;
    private Vector2 _direction = Vector2.Down;
    private Vector2? _aoePoint;

    private double _elapsed;

    /// <summary>
    /// 技能施放最短持续时间（秒），用于模拟后摇并防止“瞬间多次触发”。
    /// </summary>
    public float MinDurationSeconds { get; set; } = 0.15f;

    public PlayerSkillCastState(
        Func<SkillSlot, SkillDefinition> getSkill,
        Action<SkillDefinition, Vector2, Vector2?> spawnEffect
    )
    {
        _getSkill = getSkill ?? throw new ArgumentNullException(nameof(getSkill));
        _spawnEffect = spawnEffect ?? throw new ArgumentNullException(nameof(spawnEffect));
    }

    public void Configure(SkillSlot slot, Vector2 direction, Vector2? aoePoint)
    {
        _slot = slot;
        _direction = direction == Vector2.Zero ? Vector2.Down : direction.Normalized();
        _aoePoint = aoePoint;
    }

    public void Enter(ActorContext context)
    {
        _elapsed = 0d;
        context.IsCasting = true;
        context.CastFinishedThisFrame = false;
        context.CanMove = false;
        context.CanAttack = false;

        var def = _getSkill(_slot);
        if (!context.Mana.TryConsume(def.ManaCost))
        {
            // 防御性处理：理论上 orchestrator 已阻止。
            context.IsCasting = false;
            context.CanMove = true;
            context.CanAttack = true;
            context.CastFinishedThisFrame = true;
            return;
        }

        context.Cooldowns.Start(_slot, def.CooldownSeconds);
        context.Facing = _direction;

        _spawnEffect(def, _direction, _aoePoint);
    }

    public void Exit(ActorContext context)
    {
        context.IsCasting = false;
        context.CanMove = true;
        context.CanAttack = true;
    }

    public void Update(ActorContext context, double delta) { }

    public void PhysicsUpdate(ActorContext context, double delta)
    {
        _elapsed += delta;
        if (_elapsed < MinDurationSeconds)
        {
            return;
        }

        context.IsCasting = false;
        context.CanMove = true;
        context.CanAttack = true;
        context.CastFinishedThisFrame = true;
    }
}
