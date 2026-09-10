using System;
using System.Linq;
using Godot;
using GodotGameTemplate.Config;
using GodotGameTemplate.Config.Player;
using GodotGameTemplate.Game.Scenes.Items;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Combat;
using GodotGameTemplate.Gameplay.Combat.Targeting;
using GodotGameTemplate.Gameplay.Common.StateMachine;
using GodotGameTemplate.Gameplay.Input;
using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Navigation;
using GodotGameTemplate.Gameplay.Player.States;
using GodotGameTemplate.Gameplay.Session;

namespace GodotGameTemplate.Gameplay.Player;

public partial class PlayerController : CharacterBody2D
{
    private const float EvadeStaminaCost = 25f;

    [Export]
    public PlayerConfig? Config { get; set; }

    [Export]
    public PlayerAttackConfig? AttackConfig { get; set; }

    [Export]
    public PlayerAttackHitbox? AttackHitbox { get; set; }

    [Export]
    public PlayerView? View { get; set; }

    [Export]
    public ClickToMoveController? ClickToMove { get; set; }

    private readonly ActorContext _context = new();
    private readonly IIntentProvider _keyboardInput = new PlayerInputAdapter();
    private readonly ICommandProvider _mouseKeyboardInput = new MouseKeyboardInputAdapter();
    private readonly GamepadInputAdapter _gamepadInput = new();
    private readonly TouchInputAdapter _touchInput = new();
    private readonly PlayerIdleState _idleState = new();
    private readonly PlayerMoveState _moveState = new();
    private readonly PlayerEvadeState _evadeState = new();
    private readonly TargetingService _targeting = new();
    private readonly CombatOrchestrator _combat = new();
    private readonly ClickToMoveModel _clickToMoveModel = new();
    private GameSession _session = default!;

    public GameFeatures Features { get; set; } = new();

    public InventoryModel Inventory => _session.Inventory;

    public EquipmentModel Equipment => _session.Equipment;

    public int Gold
    {
        get => _session.Gold;
        set => _session.Gold = value;
    }

    /// <summary>
    /// 角色运行时上下文（用于 HUD 读取体力等信息）。
    /// </summary>
    public ActorContext ActorContext => _context;

    /// <summary>
    /// 触屏输入占位适配器。后续虚拟摇杆/按钮 UI 可直接把状态写入这里。
    /// </summary>
    public TouchInputAdapter TouchInput => _touchInput;

    private bool _wasLeftMouseDown;

    private PlayerAttackState _attackState = default!;
    private StateMachine<ActorContext> _stateMachine = default!;

    public override void _Ready()
    {
        // 供 UI 快速定位玩家。
        AddToGroup("player");

        _session =
            GetNodeOrNull<GameSession>("/root/GameSession")
            ?? throw new InvalidOperationException(
                "GameSession autoload not found. Please ensure project.godot [autoload] is configured."
            );

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

        ClickToMove ??= GetNodeOrNull<ClickToMoveController>("ClickToMove");
        if (ClickToMove != null)
        {
            ClickToMove.MaxSpeed = Config.MoveSpeed;
        }

        View?.Sync(_context);
    }

    public override void _Process(double delta)
    {
        _stateMachine.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _context.AttackFinishedThisFrame = false;
        _context.EvadeFinishedThisFrame = false;

        Config ??= new PlayerConfig();
        AttackConfig ??= new PlayerAttackConfig();

        if (ClickToMove != null)
        {
            ClickToMove.MaxSpeed = Config.MoveSpeed;
        }

        var consumedLeftClick = HandleLeftClick();
        var keyboardIntent = _keyboardInput.GetIntent();
        var mergedCommand = MergeCommands(
            _mouseKeyboardInput.GetCommand(),
            _gamepadInput.GetCommand(),
            _touchInput.GetCommand()
        );

        _context.Intent = ResolveManualIntent(
            mergedCommand,
            keyboardIntent.Move,
            _gamepadInput.MoveVector,
            _touchInput.MoveVector,
            keyboardIntent.AttackPressed,
            consumedLeftClick
        );

        _combat.AttackRange = AttackConfig.AttackRange;
        if (!_context.IsEvading)
        {
            ApplyCombatOrchestration();
        }

        ApplyClickToMoveIntentOverride();

        if (AttackHitbox != null)
        {
            AttackHitbox.AttackId = AttackConfig!.AttackId;
        }

        _context.Stamina.Tick((float)delta);
        var evadePressed = mergedCommand.EvadePressed;
        if (evadePressed && !_context.IsAttacking && !_context.IsEvading)
        {
            if (_context.Stamina.TryConsume(EvadeStaminaCost))
            {
                _stateMachine.ChangeState(_evadeState);
            }
        }

        if (
            !_context.IsAttacking
            && !_context.IsEvading
            && _context.CanAttack
            && _context.Intent.AttackPressed
        )
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

        if (_context.EvadeFinishedThisFrame)
        {
            _stateMachine.ChangeState(_context.Intent.HasMoveInput ? _moveState : _idleState);
        }
        else if (_context.AttackFinishedThisFrame)
        {
            _stateMachine.ChangeState(_context.Intent.HasMoveInput ? _moveState : _idleState);
        }
        else if (!_context.IsAttacking && !_context.IsEvading)
        {
            _stateMachine.ChangeState(_context.HasMoveInput ? _moveState : _idleState);
        }

        Velocity = _context.Velocity;
        MoveAndSlide();

        _context.Velocity = Velocity;
        View?.Sync(_context);
    }

    private bool HandleLeftClick()
    {
        var isDown = Godot.Input.IsMouseButtonPressed(MouseButton.Left);
        var justPressed = isDown && !_wasLeftMouseDown;
        _wasLeftMouseDown = isDown;

        if (!justPressed)
        {
            return false;
        }

        var mousePos = GetGlobalMousePosition();
        var space = GetWorld2D().DirectSpaceState;

        var query = new PhysicsPointQueryParameters2D
        {
            Position = mousePos,
            CollideWithAreas = true,
            CollideWithBodies = true,
        };

        var results = space.IntersectPoint(query, maxResults: 16);

        if (Features.EnableLoot && Features.EnableInventory)
        {
            var loot = results
                .Select(r => r["collider"].AsGodotObject())
                .OfType<Node>()
                .Select(TryResolveLootPickupNode)
                .FirstOrDefault(n => n != null);

            if (loot != null)
            {
                return HandleLootClick(loot);
            }
        }
        var target = results
            .Select(r => r["collider"].AsGodotObject())
            .OfType<Node>()
            .Select(TryResolveTargetableNode)
            .FirstOrDefault(n => n != null);

        if (target != null)
        {
            _targeting.SetTarget(target.GetInstanceId());
            ClearClickToMoveDestination();
        }
        else
        {
            _targeting.ClearTarget();
            _clickToMoveModel.SetDestination(mousePos);
            ClickToMove?.SetDestination(mousePos);
        }

        return true;
    }

    private bool HandleLootClick(LootPickup loot)
    {
        var ok = Inventory.TryAdd(loot.Item);
        if (ok)
        {
            loot.Pick();
        }

        return true;
    }

    private void ApplyCombatOrchestration()
    {
        if (!TryResolveCurrentTarget(out var targetNode))
        {
            return;
        }

        var distance = GlobalPosition.DistanceTo(targetNode.GlobalPosition);
        var decision = _combat.Evaluate(
            new CombatSnapshot(
                HasTarget: true,
                IsTargetValid: true,
                DistanceToTarget: distance,
                IsAttacking: _context.IsAttacking,
                CanAttack: _context.CanAttack
            )
        );

        if (decision.ClearTarget)
        {
            _targeting.ClearTarget();
            ClearClickToMoveDestination();
            return;
        }

        if (decision.ShouldChase)
        {
            _clickToMoveModel.SetDestination(targetNode.GlobalPosition);
            ClickToMove?.SetDestination(targetNode.GlobalPosition);
            return;
        }

        ClearClickToMoveDestination();
        if (decision.ShouldAttack)
        {
            FaceTowards(targetNode.GlobalPosition);
            _context.Intent = _context.Intent with { AttackPressed = true };
        }
    }

    private void ApplyClickToMoveIntentOverride()
    {
        if (_clickToMoveModel.Destination == null)
        {
            return;
        }

        if (_clickToMoveModel.IsArrived(GlobalPosition))
        {
            _clickToMoveModel.ClearDestination();
            ClickToMove?.Stop();
            return;
        }

        // 键盘移动优先，只有在没有手动移动输入时才启用点击移动输出。
        if (_context.Intent.HasMoveInput || ClickToMove == null)
        {
            return;
        }

        var desired = ClickToMove.DesiredVelocity;
        var move = desired == Vector2.Zero ? Vector2.Zero : desired.Normalized();
        _context.Intent = _context.Intent with { Move = move };
    }

    private static PlayerCommand MergeCommands(
        PlayerCommand mouseKeyboard,
        PlayerCommand gamepad,
        PlayerCommand touch
    )
    {
        return new PlayerCommand(
            ClickMoveDestination: mouseKeyboard.ClickMoveDestination
                ?? gamepad.ClickMoveDestination
                ?? touch.ClickMoveDestination,
            ClickTargetInstanceId: mouseKeyboard.ClickTargetInstanceId
                ?? gamepad.ClickTargetInstanceId
                ?? touch.ClickTargetInstanceId,
            EvadePressed: mouseKeyboard.EvadePressed || gamepad.EvadePressed || touch.EvadePressed,
            InteractPressed: mouseKeyboard.InteractPressed
                || gamepad.InteractPressed
                || touch.InteractPressed,
            ToggleInventoryPressed: mouseKeyboard.ToggleInventoryPressed
                || gamepad.ToggleInventoryPressed
                || touch.ToggleInventoryPressed,
            PrimaryPressed: mouseKeyboard.PrimaryPressed
                || gamepad.PrimaryPressed
                || touch.PrimaryPressed,
            SecondaryPressed: mouseKeyboard.SecondaryPressed
                || gamepad.SecondaryPressed
                || touch.SecondaryPressed,
            Skill1Pressed: mouseKeyboard.Skill1Pressed
                || gamepad.Skill1Pressed
                || touch.Skill1Pressed,
            Skill2Pressed: mouseKeyboard.Skill2Pressed
                || gamepad.Skill2Pressed
                || touch.Skill2Pressed,
            Skill3Pressed: mouseKeyboard.Skill3Pressed
                || gamepad.Skill3Pressed
                || touch.Skill3Pressed,
            Skill4Pressed: mouseKeyboard.Skill4Pressed
                || gamepad.Skill4Pressed
                || touch.Skill4Pressed
        );
    }

    private static ActorIntent ResolveManualIntent(
        PlayerCommand mergedCommand,
        Vector2 keyboardMove,
        Vector2 gamepadMove,
        Vector2 touchMove,
        bool keyboardAttackPressed,
        bool consumedLeftClick
    )
    {
        var move = keyboardMove;
        if (move.LengthSquared() <= 0f)
        {
            move = gamepadMove.LengthSquared() > 0f ? gamepadMove : touchMove;
        }

        if (move.LengthSquared() > 1f)
        {
            move = move.Normalized();
        }

        var attackPressed =
            !consumedLeftClick && (keyboardAttackPressed || mergedCommand.PrimaryPressed);

        return new ActorIntent(move, attackPressed);
    }

    private bool TryResolveCurrentTarget(out Node2D targetNode)
    {
        targetNode = null!;

        if (_targeting.CurrentTargetInstanceId is not { } id)
        {
            return false;
        }

        if (
            GodotObject.InstanceFromId(id) is not Node2D candidate
            || !candidate.IsInGroup("targetable")
        )
        {
            _targeting.ClearTarget();
            ClearClickToMoveDestination();
            return false;
        }

        targetNode = candidate;
        return true;
    }

    private void ClearClickToMoveDestination()
    {
        _clickToMoveModel.ClearDestination();
        ClickToMove?.Stop();
    }

    private void FaceTowards(Vector2 worldPosition)
    {
        var direction = worldPosition - GlobalPosition;
        if (direction == Vector2.Zero)
        {
            return;
        }

        _context.Facing = direction.Normalized();
    }

    private static Node? TryResolveTargetableNode(Node startNode)
    {
        Node? current = startNode;
        while (current != null)
        {
            if (current.IsInGroup("targetable"))
            {
                return current;
            }

            current = current.GetParent();
        }

        return null;
    }

    private static LootPickup? TryResolveLootPickupNode(Node startNode)
    {
        Node? current = startNode;
        while (current != null)
        {
            if (current is LootPickup lp)
            {
                return lp;
            }

            current = current.GetParent();
        }

        return null;
    }
}
