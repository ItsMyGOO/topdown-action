using Godot;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;

namespace GodotGameTemplate.Gameplay.Player.States;

/// <summary>
/// 玩家翻滚（Evade）状态：
/// <para>
/// - 进入时锁定移动/攻击
/// - 持续时间内按固定速度沿当前朝向位移
/// - 到达最小时长后结束（通过 <see cref="ActorContext.EvadeFinishedThisFrame"/> 通知控制器切回常规状态）
/// </para>
/// </summary>
public sealed class PlayerEvadeState : IState<ActorContext>
{
    private readonly float _duration;
    private readonly float _speed;
    private double _elapsed;

    public PlayerEvadeState(float duration = 0.22f, float speed = 220f)
    {
        _duration = Mathf.Max(0.05f, duration);
        _speed = Mathf.Max(0f, speed);
    }

    public void Enter(ActorContext context)
    {
        _elapsed = 0d;
        context.IsEvading = true;
        context.EvadeFinishedThisFrame = false;
        context.CanAttack = false;
        context.CanMove = false;
    }

    public void Exit(ActorContext context)
    {
        context.IsEvading = false;
        context.CanAttack = true;
        context.CanMove = true;
        context.Velocity = Vector2.Zero;
    }

    public void Update(ActorContext context, double delta) { }

    public void PhysicsUpdate(ActorContext context, double delta)
    {
        _elapsed += delta;

        var facing = context.Facing == Vector2.Zero ? Vector2.Down : context.Facing.Normalized();
        context.Velocity = facing * _speed;

        if (_elapsed < _duration)
        {
            return;
        }

        context.Velocity = Vector2.Zero;
        context.EvadeFinishedThisFrame = true;
    }
}
