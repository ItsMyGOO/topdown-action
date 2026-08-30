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
    public PlayerView? View { get; set; }

    private readonly ActorContext _context = new();
    private readonly IIntentProvider _input = new PlayerInputAdapter();
    private readonly PlayerIdleState _idleState = new();
    private readonly PlayerMoveState _moveState = new();

    private StateMachine<ActorContext> _stateMachine = default!;

    public override void _Ready()
    {
        Config ??= new PlayerConfig();
        View ??= GetNodeOrNull<PlayerView>("View");

        _stateMachine = new StateMachine<ActorContext>(_context);
        _stateMachine.ChangeState(_idleState);
        View?.Sync(_context);
    }

    public override void _Process(double delta)
    {
        _stateMachine.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _context.Intent = _input.GetIntent();

        Config ??= new PlayerConfig();
        var moveInput = _context.CanMove ? _context.Intent.Move : Vector2.Zero;

        _context.Velocity = ActorMotor.UpdateVelocity(
            currentVelocity: Velocity,
            moveInput: moveInput,
            maxSpeed: Config.MoveSpeed,
            acceleration: Config.Acceleration,
            deceleration: Config.Deceleration,
            delta: (float)delta
        );

        _stateMachine.ChangeState(_context.HasMoveInput ? _moveState : _idleState);
        _stateMachine.PhysicsUpdate(delta);

        Velocity = _context.Velocity;
        MoveAndSlide();

        _context.Velocity = Velocity;
        View?.Sync(_context);
    }
}
