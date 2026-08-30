using System;
using Godot;
using GodotGameTemplate.Config.Player;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;

namespace GodotGameTemplate.Gameplay.Player.States;

public interface IPlayerAttackHitbox
{
    void Configure(Vector2 facing, float range);

    void SetActive(bool active);

    void ResetHitTargets();
}

public sealed class PlayerAttackState : IState<ActorContext>
{
    private readonly PlayerAttackConfig _config;
    private readonly IPlayerAttackHitbox _hitbox;
    private double _elapsed;

    public PlayerAttackState(PlayerAttackConfig config, IPlayerAttackHitbox hitbox)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _hitbox = hitbox ?? throw new ArgumentNullException(nameof(hitbox));
    }

    public void Enter(ActorContext context)
    {
        _elapsed = 0d;
        context.IsAttacking = true;
        context.AttackFinishedThisFrame = false;
        context.CanMove = false;
        context.AttackFacing = ResolveAttackFacing(context);
        context.Facing = context.AttackFacing;

        _hitbox.Configure(context.AttackFacing, GetAttackRange());
        _hitbox.ResetHitTargets();
        _hitbox.SetActive(false);
    }

    public void Exit(ActorContext context)
    {
        _hitbox.SetActive(false);
        context.IsAttacking = false;
        context.CanMove = true;
    }

    public void Update(ActorContext context, double delta)
    {
    }

    public void PhysicsUpdate(ActorContext context, double delta)
    {
        _elapsed += delta;

        var hitboxStart = GetHitboxStartTime();
        var hitboxEnd = GetHitboxEndTime();
        var totalDuration = GetTotalDuration();
        var isHitboxActive = _elapsed >= hitboxStart && _elapsed <= hitboxEnd;
        _hitbox.SetActive(isHitboxActive);

        if (_elapsed < totalDuration)
        {
            return;
        }

        _hitbox.SetActive(false);
        context.IsAttacking = false;
        context.CanMove = true;
        context.AttackFinishedThisFrame = true;
    }

    private static Vector2 ResolveAttackFacing(ActorContext context)
    {
        if (context.Intent.HasMoveInput)
        {
            return context.Intent.Move.Normalized();
        }

        return context.Facing == Vector2.Zero ? Vector2.Down : context.Facing.Normalized();
    }

    private float GetTotalDuration()
    {
        return Mathf.Max(0.01f, _config.TotalDuration);
    }

    private float GetHitboxStartTime()
    {
        return Mathf.Clamp(_config.HitboxStartTime, 0f, GetTotalDuration());
    }

    private float GetHitboxEndTime()
    {
        return Mathf.Clamp(_config.HitboxEndTime, GetHitboxStartTime(), GetTotalDuration());
    }

    private float GetAttackRange()
    {
        return Mathf.Max(0f, _config.AttackRange);
    }
}
