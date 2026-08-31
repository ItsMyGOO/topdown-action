using Godot;
using GodotGameTemplate.Config.Player;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Player.States;

namespace GodotGameTemplate.Tests.Gameplay.Player;

public sealed class PlayerAttackStateTests
{
    [Fact]
    public void Enter_LocksFacingDirectionFromCurrentIntent()
    {
        var context = new ActorContext
        {
            Facing = Vector2.Right,
            Intent = new ActorIntent(Vector2.Up, true),
        };

        var config = new FakeAttackConfig();
        var hitbox = new FakeAttackHitbox();
        var state = new PlayerAttackState(config, hitbox);

        state.Enter(context);

        Assert.Equal(Vector2.Up, context.AttackFacing);
        Assert.True(context.IsAttacking);
        Assert.False(hitbox.IsActive);
    }

    [Fact]
    public void PhysicsUpdate_ActivatesHitboxInsideConfiguredWindow()
    {
        var context = new ActorContext
        {
            Facing = Vector2.Right,
            Intent = new ActorIntent(Vector2.Right, true),
        };

        var config = new FakeAttackConfig
        {
            TotalDuration = 0.30f,
            HitboxStartTime = 0.10f,
            HitboxEndTime = 0.20f,
        };

        var hitbox = new FakeAttackHitbox();
        var state = new PlayerAttackState(config, hitbox);

        state.Enter(context);
        state.PhysicsUpdate(context, 0.11d);

        Assert.True(hitbox.IsActive);
    }

    [Fact]
    public void PhysicsUpdate_AfterDuration_EndsAttackAndClearsHitbox()
    {
        var context = new ActorContext
        {
            Facing = Vector2.Right,
            Intent = new ActorIntent(Vector2.Right, true),
        };

        var config = new FakeAttackConfig
        {
            TotalDuration = 0.20f,
            HitboxStartTime = 0.05f,
            HitboxEndTime = 0.10f,
        };

        var hitbox = new FakeAttackHitbox();
        var state = new PlayerAttackState(config, hitbox);

        state.Enter(context);
        state.PhysicsUpdate(context, 0.25d);

        Assert.False(context.IsAttacking);
        Assert.False(hitbox.IsActive);
        Assert.True(context.AttackFinishedThisFrame);
    }

    private sealed class FakeAttackHitbox : IPlayerAttackHitbox
    {
        public bool IsActive { get; private set; }

        public void Configure(Vector2 facing, float range)
        {
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }

        public void ResetHitTargets()
        {
        }
    }

    private sealed class FakeAttackConfig : IPlayerAttackConfig
    {
        public float TotalDuration { get; init; } = 0.25f;

        public float HitboxStartTime { get; init; } = 0.08f;

        public float HitboxEndTime { get; init; } = 0.16f;

        public float AttackRange { get; init; } = 18f;

        public string AttackId { get; init; } = "player_basic_slash";
    }
}
