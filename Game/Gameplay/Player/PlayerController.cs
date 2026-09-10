using System;
using System.Linq;
using Godot;
using GodotGameTemplate.Config;
using GodotGameTemplate.Config.Player;
using GodotGameTemplate.Game.Scenes.Items;
using GodotGameTemplate.Game.Scenes.Skills;
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
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Gameplay.Player;

public partial class PlayerController : CharacterBody2D
{
    private const float EvadeStaminaCost = 25f;
    private const string AoeIndicatorScenePath = "res://Game/Scenes/Skills/AoeIndicator.tscn";
    private const string ProjectileEffectScenePath =
        "res://Game/Scenes/Skills/ProjectileSkillEffect.tscn";
    private const string AoeStrikeEffectScenePath =
        "res://Game/Scenes/Skills/AoeStrikeSkillEffect.tscn";

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
    private readonly SkillOrchestrator _skillOrchestrator = new();
    private readonly TargetingService _targeting = new();
    private readonly CombatOrchestrator _combat = new();
    private readonly ClickToMoveModel _clickToMoveModel = new();
    private GameSession _session = default!;

    private readonly PlayerSkillCastState _skillCastState;
    private readonly PlayerAoeTargetingState _aoeTargetingState;

    private PackedScene? _aoeIndicatorScene;
    private PackedScene? _projectileEffectScene;
    private PackedScene? _aoeStrikeEffectScene;
    private AoeIndicator? _aoeIndicator;

    private PlayerCommand _currentCommand;

    private string _skillToastMessage = string.Empty;
    private double _skillToastRemainingSeconds;

    public string SkillToastMessage =>
        _skillToastRemainingSeconds > 0d ? _skillToastMessage : string.Empty;

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

        // Skill scenes
        _aoeIndicatorScene = GD.Load<PackedScene>(AoeIndicatorScenePath);
        _projectileEffectScene = GD.Load<PackedScene>(ProjectileEffectScenePath);
        _aoeStrikeEffectScene = GD.Load<PackedScene>(AoeStrikeEffectScenePath);

        View?.Sync(_context);
    }

    public PlayerController()
    {
        // 注意：这里的委托会捕获 this，因此必须在运行时访问（Enter/Update）时才会使用到场景树数据。
        _skillCastState = new PlayerSkillCastState(
            getSkill: slot => SkillDatabase.DefaultBySlot[slot],
            spawnEffect: SpawnSkillEffect
        );

        _aoeTargetingState = new PlayerAoeTargetingState(
            getMouseWorld: () => GetGlobalMousePosition(),
            getAimVector: () => _currentCommand.AimVector,
            getConfirmPressed: () => _currentCommand.ConfirmPressed,
            getCancelPressed: () => _currentCommand.CancelPressed,
            getPlayerPosition: () => GlobalPosition,
            getMaxRange: () => SkillDatabase.DefaultBySlot[SkillSlot.Secondary].Range,
            getRadius: () => SkillDatabase.DefaultBySlot[SkillSlot.Secondary].AoeRadius,
            getOrCreateIndicator: GetOrCreateAoeIndicator
        );
    }

    public override void _Process(double delta)
    {
        _stateMachine.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_skillToastRemainingSeconds > 0d)
        {
            _skillToastRemainingSeconds = Math.Max(0d, _skillToastRemainingSeconds - delta);
        }

        _context.AttackFinishedThisFrame = false;
        _context.EvadeFinishedThisFrame = false;
        _context.CastFinishedThisFrame = false;
        _context.TargetingFinishedThisFrame = false;

        Config ??= new PlayerConfig();
        AttackConfig ??= new PlayerAttackConfig();

        if (ClickToMove != null)
        {
            ClickToMove.MaxSpeed = Config.MoveSpeed;
        }

        // 瞄准期间，左键用于确认释放，不再用于拾取/选中/点击移动。
        var consumedLeftClick = !_context.IsTargeting && HandleLeftClick();
        var keyboardIntent = _keyboardInput.GetIntent();
        _currentCommand = MergeCommands(
            _mouseKeyboardInput.GetCommand(),
            _gamepadInput.GetCommand(),
            _touchInput.GetCommand()
        );

        _context.Intent = ResolveManualIntent(
            _currentCommand,
            keyboardIntent.Move,
            _gamepadInput.MoveVector,
            _touchInput.MoveVector,
            keyboardIntent.AttackPressed,
            consumedLeftClick
        );

        _combat.AttackRange = AttackConfig.AttackRange;
        if (!_context.IsEvading && !_context.IsCasting && !_context.IsTargeting)
        {
            ApplyCombatOrchestration();
        }

        ApplyClickToMoveIntentOverride();

        if (AttackHitbox != null)
        {
            AttackHitbox.AttackId = AttackConfig!.AttackId;
        }

        _context.Stamina.Tick((float)delta);
        if (Features.EnableSkills)
        {
            _context.Mana.Tick((float)delta);
            _context.Cooldowns.Tick((float)delta);
        }
        var evadePressed = _currentCommand.EvadePressed;
        if (
            evadePressed
            && !_context.IsAttacking
            && !_context.IsEvading
            && !_context.IsCasting
            && !_context.IsTargeting
        )
        {
            if (_context.Stamina.TryConsume(EvadeStaminaCost))
            {
                _stateMachine.ChangeState(_evadeState);
            }
        }

        if (Features.EnableSkills)
        {
            ApplySkillInput();
        }

        if (
            !_context.IsAttacking
            && !_context.IsEvading
            && !_context.IsCasting
            && !_context.IsTargeting
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

        if (_context.TargetingFinishedThisFrame)
        {
            if (_aoeTargetingState.WasConfirmed)
            {
                StartSecondaryCastFromTargetingPoint(_aoeTargetingState.SelectedPoint);
            }
            else
            {
                _stateMachine.ChangeState(_context.Intent.HasMoveInput ? _moveState : _idleState);
            }
        }
        else if (_context.CastFinishedThisFrame)
        {
            _stateMachine.ChangeState(_context.Intent.HasMoveInput ? _moveState : _idleState);
        }
        else if (_context.EvadeFinishedThisFrame)
        {
            _stateMachine.ChangeState(_context.Intent.HasMoveInput ? _moveState : _idleState);
        }
        else if (_context.AttackFinishedThisFrame)
        {
            _stateMachine.ChangeState(_context.Intent.HasMoveInput ? _moveState : _idleState);
        }
        else if (
            !_context.IsAttacking
            && !_context.IsEvading
            && !_context.IsCasting
            && !_context.IsTargeting
        )
        {
            _stateMachine.ChangeState(_context.HasMoveInput ? _moveState : _idleState);
        }

        Velocity = _context.Velocity;
        MoveAndSlide();

        _context.Velocity = Velocity;
        View?.Sync(_context);
    }

    private void ApplySkillInput()
    {
        // 瞄准/施法/攻击/翻滚期间不再触发新的技能。
        var isBusy =
            _context.IsAttacking
            || _context.IsEvading
            || _context.IsCasting
            || _context.IsTargeting;

        var decision = _skillOrchestrator.Evaluate(
            _currentCommand,
            SkillDatabase.DefaultBySlot,
            _context.Mana,
            _context.Cooldowns,
            isBusy
        );

        switch (decision.Kind)
        {
            case SkillDecisionKind.StartAoeTargeting:
                // Secondary：进入选点模式
                ClearClickToMoveDestination();
                _stateMachine.ChangeState(_aoeTargetingState);
                break;
            case SkillDecisionKind.StartInstantCast:
                // Primary 仍由原普攻系统处理，避免重复。
                if (decision.Slot == SkillSlot.Primary)
                {
                    break;
                }

                StartInstantCast(decision.Slot);
                break;
            case SkillDecisionKind.Reject:
                ShowSkillToast(decision.RejectReason);
                break;
            default:
                break;
        }
    }

    private void ShowSkillToast(SkillRejectReason reason)
    {
        _skillToastMessage = reason switch
        {
            SkillRejectReason.Cooldown => "技能冷却中",
            SkillRejectReason.NotEnoughMana => "法力不足",
            SkillRejectReason.Busy => "当前无法施放",
            _ => "无法施放",
        };
        _skillToastRemainingSeconds = 1.0d;
    }

    private void StartInstantCast(SkillSlot slot)
    {
        var def = SkillDatabase.DefaultBySlot[slot];
        var direction = ResolveCastDirection();
        Vector2? point = null;

        if (def.EffectKind == SkillEffectKind.AoeStrike)
        {
            point = ResolveAoePoint(def, direction);
        }

        ClearClickToMoveDestination();
        _skillCastState.Configure(slot, direction, point);
        _stateMachine.ChangeState(_skillCastState);
    }

    private void StartSecondaryCastFromTargetingPoint(Vector2 point)
    {
        var direction = point - GlobalPosition;
        if (direction == Vector2.Zero)
        {
            direction = _context.Facing;
        }

        ClearClickToMoveDestination();
        _skillCastState.Configure(SkillSlot.Secondary, direction, point);
        _stateMachine.ChangeState(_skillCastState);
    }

    private Vector2 ResolveCastDirection()
    {
        var aim = _currentCommand.AimVector;
        if (aim.LengthSquared() > 0.01f)
        {
            return aim.Normalized();
        }

        var mouseDir = GetGlobalMousePosition() - GlobalPosition;
        if (mouseDir.LengthSquared() > 0.01f)
        {
            return mouseDir.Normalized();
        }

        return _context.Facing == Vector2.Zero ? Vector2.Down : _context.Facing.Normalized();
    }

    private Vector2 ResolveAoePoint(SkillDefinition def, Vector2 direction)
    {
        // 优先：有合法目标且在范围内 -> 目标落点
        if (TryResolveCurrentTarget(out var targetNode))
        {
            var d = GlobalPosition.DistanceTo(targetNode.GlobalPosition);
            if (d <= def.Range)
            {
                return targetNode.GlobalPosition;
            }
        }

        // 否则：朝方向落点并 clamp 到 range
        var range = Mathf.Max(0f, def.Range);
        return GlobalPosition + direction.Normalized() * range;
    }

    private AoeIndicator? GetOrCreateAoeIndicator()
    {
        if (_aoeIndicator != null && GodotObject.IsInstanceValid(_aoeIndicator))
        {
            return _aoeIndicator;
        }

        if (!Features.EnableAoeIndicator || _aoeIndicatorScene == null)
        {
            return null;
        }

        var instance = _aoeIndicatorScene.Instantiate<AoeIndicator>();
        _aoeIndicator = instance;

        // 挂到玩家父节点，确保与世界坐标一致
        (GetParent() ?? this).AddChild(instance);
        return instance;
    }

    private void SpawnSkillEffect(SkillDefinition def, Vector2 direction, Vector2? aoePoint)
    {
        // 技能效果尽量挂到世界节点（玩家父节点）。
        var parent = GetParent() ?? this;

        switch (def.EffectKind)
        {
            case SkillEffectKind.Projectile:
                if (_projectileEffectScene == null)
                {
                    return;
                }

                var proj = _projectileEffectScene.Instantiate<ProjectileSkillEffect>();
                proj.GlobalPosition = GlobalPosition;
                proj.Direction = direction;
                proj.AttackId = def.SkillId;
                parent.AddChild(proj);
                break;
            case SkillEffectKind.AoeStrike:
                if (_aoeStrikeEffectScene == null || aoePoint == null)
                {
                    return;
                }

                var aoe = _aoeStrikeEffectScene.Instantiate<AoeStrikeSkillEffect>();
                aoe.GlobalPosition = aoePoint.Value;
                aoe.Radius = def.AoeRadius;
                aoe.AttackId = def.SkillId;
                parent.AddChild(aoe);
                break;
            default:
                break;
        }
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
            AimVector: gamepad.AimVector.LengthSquared() > 0f ? gamepad.AimVector
                : touch.AimVector.LengthSquared() > 0f ? touch.AimVector
                : Vector2.Zero,
            EvadePressed: mouseKeyboard.EvadePressed || gamepad.EvadePressed || touch.EvadePressed,
            InteractPressed: mouseKeyboard.InteractPressed
                || gamepad.InteractPressed
                || touch.InteractPressed,
            ToggleInventoryPressed: mouseKeyboard.ToggleInventoryPressed
                || gamepad.ToggleInventoryPressed
                || touch.ToggleInventoryPressed,
            ConfirmPressed: mouseKeyboard.ConfirmPressed
                || gamepad.ConfirmPressed
                || touch.ConfirmPressed,
            CancelPressed: mouseKeyboard.CancelPressed
                || gamepad.CancelPressed
                || touch.CancelPressed,
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
