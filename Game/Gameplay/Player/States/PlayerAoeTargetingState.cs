using System;
using Godot;
using GodotGameTemplate.Game.Scenes.Skills;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;

namespace GodotGameTemplate.Gameplay.Player.States;

/// <summary>
/// 地面选点瞄准状态（用于 Secondary）。
/// <para>
/// - 显示指示器
/// - 鼠标跟随；手柄右摇杆增量移动
/// - Confirm/Cancel 结束瞄准，并记录是否确认
/// </para>
/// </summary>
public sealed class PlayerAoeTargetingState : IState<ActorContext>
{
    private const float AimMoveSpeed = 320f;

    private readonly Func<Vector2> _getMouseWorld;
    private readonly Func<Vector2> _getAimVector;
    private readonly Func<bool> _getConfirmPressed;
    private readonly Func<bool> _getCancelPressed;
    private readonly Func<Vector2> _getPlayerPosition;
    private readonly Func<float> _getMaxRange;
    private readonly Func<float> _getRadius;
    private readonly Func<AoeIndicator?> _getOrCreateIndicator;

    private AoeIndicator? _indicator;
    private Vector2 _point;

    public PlayerAoeTargetingState(
        Func<Vector2> getMouseWorld,
        Func<Vector2> getAimVector,
        Func<bool> getConfirmPressed,
        Func<bool> getCancelPressed,
        Func<Vector2> getPlayerPosition,
        Func<float> getMaxRange,
        Func<float> getRadius,
        Func<AoeIndicator?> getOrCreateIndicator
    )
    {
        _getMouseWorld = getMouseWorld ?? throw new ArgumentNullException(nameof(getMouseWorld));
        _getAimVector = getAimVector ?? throw new ArgumentNullException(nameof(getAimVector));
        _getConfirmPressed =
            getConfirmPressed ?? throw new ArgumentNullException(nameof(getConfirmPressed));
        _getCancelPressed =
            getCancelPressed ?? throw new ArgumentNullException(nameof(getCancelPressed));
        _getPlayerPosition =
            getPlayerPosition ?? throw new ArgumentNullException(nameof(getPlayerPosition));
        _getMaxRange = getMaxRange ?? throw new ArgumentNullException(nameof(getMaxRange));
        _getRadius = getRadius ?? throw new ArgumentNullException(nameof(getRadius));
        _getOrCreateIndicator =
            getOrCreateIndicator ?? throw new ArgumentNullException(nameof(getOrCreateIndicator));
    }

    /// <summary>
    /// 本次瞄准结束时是否为“确认释放”。
    /// </summary>
    public bool WasConfirmed { get; private set; }

    /// <summary>
    /// 当前选中的世界坐标落点。
    /// </summary>
    public Vector2 SelectedPoint => _point;

    public void Enter(ActorContext context)
    {
        WasConfirmed = false;
        context.IsTargeting = true;
        context.TargetingFinishedThisFrame = false;
        context.CanMove = false;
        context.CanAttack = false;

        _indicator = _getOrCreateIndicator();
        if (_indicator != null)
        {
            _indicator.Visible = true;
            _indicator.SetRadius(_getRadius());
        }

        _point = ClampToRange(_getMouseWorld());
        if (_indicator != null)
        {
            _indicator.GlobalPosition = _point;
        }
    }

    public void Exit(ActorContext context)
    {
        context.IsTargeting = false;
        context.CanMove = true;
        context.CanAttack = true;

        if (_indicator != null)
        {
            _indicator.Visible = false;
        }
    }

    public void Update(ActorContext context, double delta)
    {
        var aim = _getAimVector();
        if (aim.LengthSquared() > 0.01f)
        {
            _point += aim.Normalized() * AimMoveSpeed * (float)delta;
        }
        else
        {
            _point = _getMouseWorld();
        }

        _point = ClampToRange(_point);
        if (_indicator != null)
        {
            _indicator.GlobalPosition = _point;
        }

        if (_getCancelPressed())
        {
            WasConfirmed = false;
            context.TargetingFinishedThisFrame = true;
        }
    }

    public void PhysicsUpdate(ActorContext context, double delta)
    {
        if (_getConfirmPressed())
        {
            WasConfirmed = true;
            context.TargetingFinishedThisFrame = true;
        }
    }

    private Vector2 ClampToRange(Vector2 point)
    {
        var playerPos = _getPlayerPosition();
        var maxRange = Mathf.Max(0f, _getMaxRange());
        var offset = point - playerPos;
        if (offset.Length() <= maxRange)
        {
            return point;
        }

        return playerPos + offset.Normalized() * maxRange;
    }
}
