using System;
using Godot;
using GodotGameTemplate.Config.Player;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;
using GodotGameTemplate.Gameplay.Input;
using GodotGameTemplate.Gameplay.Player.States;

namespace GodotGameTemplate.Gameplay.Player;

public partial class PlayerController : CharacterBody2D
{
    [Export]
    public PlayerConfig? Config { get; set; }

    [Export]
    public PlayerAttackConfig? AttackConfig { get; set; }

    [Export]
    public PlayerAttackHitbox? AttackHitbox { get; set; }

    [Export]
    public PlayerView? View { get; set; }

    private readonly ActorContext _context = new();
    private readonly IIntentProvider _input = new PlayerInputAdapter();
    private readonly PlayerIdleState _idleState = new();
    private readonly PlayerMoveState _moveState = new();

    private PlayerAttackState _attackState = default!;
    private StateMachine<ActorContext> _stateMachine = default!;

    public override void _Ready()
    {
        Config ??= new PlayerConfig();
        AttackConfig ??= new PlayerAttackConfig();
        AttackHitbox ??= GetNodeOrNull<PlayerAttackHitbox>("AttackHitbox");
        View ??= GetNodeOrNull<PlayerView>("View");

        if (AttackHitbox == null)
        {
            throw new InvalidOperationException(
                "PlayerController requires an AttackHitbox child node."
            );
        }

        AttackHitbox.SourceNode = this;
        AttackHitbox.AttackId = AttackConfig.AttackId;

        _stateMachine = new StateMachine<ActorContext>(_context);
        _attackState = new PlayerAttackState(AttackConfig, AttackHitbox);
        _stateMachine.ChangeState(_idleState);
        View?.Sync(_context);
    }

    public override void _Process(double delta)
    {
        _stateMachine.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _context.AttackFinishedThisFrame = false;
        _context.Intent = _input.GetIntent();

        Config ??= new PlayerConfig();
        if (AttackHitbox != null)
        {
            AttackHitbox.AttackId = AttackConfig!.AttackId;
        }

        if (!_context.IsAttacking && _context.CanAttack && _context.Intent.AttackPressed)
        {
            _stateMachine.ChangeState(_attackState);
        }

        if (_context.CanMove)
        {
            _context.Velocity = ActorMotor.UpdateVelocity(
                currentVelocity: Velocity,
                moveInput: _context.Intent.Move,
                maxSpeed: Config.MoveSpeed,
                acceleration: Config.Acceleration,
                deceleration: Config.Deceleration,
                delta: (float)delta
            );
        }
        else
        {
            _context.Velocity = Vector2.Zero;
        }

        _stateMachine.PhysicsUpdate(delta);

        if (_context.AttackFinishedThisFrame)
        {
            _stateMachine.ChangeState(_context.Intent.HasMoveInput ? _moveState : _idleState);
        }
        else if (!_context.IsAttacking)
        {
            _stateMachine.ChangeState(_context.HasMoveInput ? _moveState : _idleState);
        }

        Velocity = _context.Velocity;
        MoveAndSlide();

        _context.Velocity = Velocity;
        View?.Sync(_context);
    }
}
