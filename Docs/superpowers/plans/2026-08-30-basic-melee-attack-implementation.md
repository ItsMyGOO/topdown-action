# Basic Melee Attack Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the first complete combat slice for the Godot top-down action framework: player melee attack input, attack state, hitbox timing, hit receiver contract, and a training dummy that proves the hit loop works.

**Architecture:** Extend the current `input -> intent -> logic -> presentation` pipeline instead of creating a parallel combat stack. Keep attack rules in focused C# modules, use a dedicated player attack state for timing and lock direction, and use a Godot `Area2D` hitbox as the scene-facing adapter that reports hits through a reusable `IHitReceiver` contract.

**Tech Stack:** Godot 4.2, C#, `CharacterBody2D`, `Area2D`, Godot `Resource`, existing lightweight FSM, xUnit test project, Markdown docs under `Docs/`

---

## File Structure

### Create

- `Game/Gameplay/Actors/Combat/HitContext.cs`
- `Game/Gameplay/Actors/Combat/IHitReceiver.cs`
- `Game/Gameplay/Player/PlayerAttackHitbox.cs`
- `Game/Gameplay/Player/States/PlayerAttackState.cs`
- `Game/Gameplay/TrainingDummy/TrainingDummyController.cs`
- `Game/Config/Player/PlayerAttackConfig.cs`
- `Docs/Modules/combat-basics.md`
- `Tests/Gameplay/Player/PlayerAttackStateTests.cs`

### Modify

- `Game/Gameplay/Actors/ActorIntent.cs`
- `Game/Gameplay/Actors/ActorContext.cs`
- `Game/Gameplay/Input/PlayerInputAdapter.cs`
- `Game/Gameplay/Player/PlayerController.cs`
- `Game/Scenes/Player/Player.tscn`
- `Game/Scenes/Main/Main.tscn`
- `Docs/Modules/player-controller.md`
- `project.godot`

## Task Breakdown

### Task 1: Extend Input Intent For Primary Attack

**Files:**
- Modify: `project.godot`
- Modify: `Game/Gameplay/Actors/ActorIntent.cs`
- Modify: `Game/Gameplay/Input/PlayerInputAdapter.cs`

- [ ] **Step 1: Add the attack action to the Godot input map**

```ini
attack_primary={
"deadzone": 0.2,
"events": [Object(InputEventKey,"device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":74,"key_label":0,"unicode":106,"location":0,"echo":false,"script":null), Object(InputEventMouseButton,"device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"button_mask":0,"position":Vector2(0, 0),"global_position":Vector2(0, 0),"factor":1.0,"button_index":1,"canceled":false,"pressed":false,"double_click":false,"script":null)]
}
```

- [ ] **Step 2: Expand `ActorIntent` to carry an attack request**

```csharp
using Godot;

namespace GodotGameTemplate.Gameplay.Actors;

public readonly record struct ActorIntent(Vector2 Move, bool AttackPressed)
{
    public bool HasMoveInput => Move.LengthSquared() > 0f;

    public static ActorIntent None => new(Vector2.Zero, false);
}
```

- [ ] **Step 3: Update `PlayerInputAdapter` to emit the attack request**

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Actors;

namespace GodotGameTemplate.Gameplay.Input;

public sealed class PlayerInputAdapter : IIntentProvider
{
    public ActorIntent GetIntent()
    {
        var move = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var attackPressed = Input.IsActionJustPressed("attack_primary");
        return new ActorIntent(move, attackPressed);
    }
}
```

- [ ] **Step 4: Run a project syntax check if Godot is available**

Run: `godot4 --headless --path /workspace --quit`
Expected: PASS without input map parse errors

- [ ] **Step 5: Commit**

```bash
git add /workspace/project.godot /workspace/Game/Gameplay/Actors/ActorIntent.cs /workspace/Game/Gameplay/Input/PlayerInputAdapter.cs
git commit -m "feat: add primary attack input intent"
```

### Task 2: Write Failing Tests For Attack State Timing

**Files:**
- Create: `Tests/Gameplay/Player/PlayerAttackStateTests.cs`

- [ ] **Step 1: Write a failing test for attack direction locking**

```csharp
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

        var config = new PlayerAttackConfig();
        var state = new PlayerAttackState(config);

        state.Enter(context);

        Assert.Equal(Vector2.Up, context.AttackFacing);
        Assert.True(context.IsAttacking);
    }
}
```

- [ ] **Step 2: Write a failing test for hit window activation**

```csharp
[Fact]
public void PhysicsUpdate_ActivatesHitboxInsideConfiguredWindow()
{
    var context = new ActorContext
    {
        Facing = Vector2.Right,
        Intent = new ActorIntent(Vector2.Right, true),
    };

    var config = new PlayerAttackConfig
    {
        TotalDuration = 0.30f,
        HitboxStartTime = 0.10f,
        HitboxEndTime = 0.20f,
    };

    var hitbox = new FakeAttackHitbox();
    var state = new PlayerAttackState(config, hitbox);

    state.Enter(context);
    state.PhysicsUpdate(context, 0.11);

    Assert.True(hitbox.IsActive);
}
```

- [ ] **Step 3: Write a failing test for attack completion**

```csharp
[Fact]
public void PhysicsUpdate_AfterDuration_EndsAttackAndClearsHitbox()
{
    var context = new ActorContext
    {
        Facing = Vector2.Right,
        Intent = new ActorIntent(Vector2.Right, true),
    };

    var config = new PlayerAttackConfig
    {
        TotalDuration = 0.20f,
        HitboxStartTime = 0.05f,
        HitboxEndTime = 0.10f,
    };

    var hitbox = new FakeAttackHitbox();
    var state = new PlayerAttackState(config, hitbox);

    state.Enter(context);
    state.PhysicsUpdate(context, 0.25);

    Assert.False(context.IsAttacking);
    Assert.False(hitbox.IsActive);
    Assert.True(context.AttackFinishedThisFrame);
}
```

- [ ] **Step 4: Include the minimal fake hitbox used by the tests**

```csharp
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
```

- [ ] **Step 5: Run the attack-state tests to confirm they fail**

Run: `dotnet test /workspace/Tests/GodotGameTemplate.Tests.csproj --filter FullyQualifiedName~PlayerAttackStateTests`
Expected: FAIL because `PlayerAttackState`, `PlayerAttackConfig`, and `IPlayerAttackHitbox` do not exist yet

- [ ] **Step 6: Commit**

```bash
git add /workspace/Tests/Gameplay/Player/PlayerAttackStateTests.cs
git commit -m "test: add failing attack state tests"
```

### Task 3: Implement Attack Config And Combat Contracts

**Files:**
- Create: `Game/Gameplay/Actors/Combat/HitContext.cs`
- Create: `Game/Gameplay/Actors/Combat/IHitReceiver.cs`
- Create: `Game/Config/Player/PlayerAttackConfig.cs`
- Modify: `Game/Gameplay/Actors/ActorContext.cs`

- [ ] **Step 1: Add the reusable hit context**

```csharp
using Godot;

namespace GodotGameTemplate.Gameplay.Actors.Combat;

public readonly record struct HitContext(Node Source, Vector2 Direction, string AttackId);
```

- [ ] **Step 2: Add the reusable hit receiver interface**

```csharp
namespace GodotGameTemplate.Gameplay.Actors.Combat;

public interface IHitReceiver
{
    void ReceiveHit(HitContext hit);
}
```

- [ ] **Step 3: Add player attack timing and range config**

```csharp
using Godot;

namespace GodotGameTemplate.Config.Player;

[GlobalClass]
public partial class PlayerAttackConfig : Resource
{
    [Export] public float TotalDuration { get; set; } = 0.25f;
    [Export] public float HitboxStartTime { get; set; } = 0.08f;
    [Export] public float HitboxEndTime { get; set; } = 0.16f;
    [Export] public float AttackRange { get; set; } = 18f;
    [Export] public string AttackId { get; set; } = "player_basic_slash";
}
```

- [ ] **Step 4: Extend `ActorContext` with attack runtime state**

```csharp
using Godot;

namespace GodotGameTemplate.Gameplay.Actors;

public sealed class ActorContext
{
    public ActorIntent Intent { get; set; } = ActorIntent.None;
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public Vector2 Facing { get; set; } = Vector2.Down;
    public bool CanMove { get; set; } = true;
    public bool CanAttack { get; set; } = true;
    public bool IsAttacking { get; set; }
    public Vector2 AttackFacing { get; set; } = Vector2.Down;
    public bool AttackFinishedThisFrame { get; set; }

    public bool HasMoveInput => CanMove && Intent.HasMoveInput;
}
```

- [ ] **Step 5: Run the tests again to confirm remaining failures are now about state implementation**

Run: `dotnet test /workspace/Tests/GodotGameTemplate.Tests.csproj --filter FullyQualifiedName~PlayerAttackStateTests`
Expected: FAIL because `PlayerAttackState` and `IPlayerAttackHitbox` still do not exist

- [ ] **Step 6: Commit**

```bash
git add /workspace/Game/Gameplay/Actors/Combat /workspace/Game/Config/Player/PlayerAttackConfig.cs /workspace/Game/Gameplay/Actors/ActorContext.cs
git commit -m "feat: add combat contracts and attack config"
```

### Task 4: Implement The Player Attack State

**Files:**
- Create: `Game/Gameplay/Player/States/PlayerAttackState.cs`
- Modify: `Tests/Gameplay/Player/PlayerAttackStateTests.cs`

- [ ] **Step 1: Add the hitbox interface inside the attack state file**

```csharp
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
```

- [ ] **Step 2: Implement `PlayerAttackState` with timing, lock direction, and hitbox activation**

```csharp
using Godot;
using GodotGameTemplate.Config.Player;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;

namespace GodotGameTemplate.Gameplay.Player.States;

public sealed class PlayerAttackState : IState<ActorContext>
{
    private readonly PlayerAttackConfig _config;
    private readonly IPlayerAttackHitbox _hitbox;
    private double _elapsed;

    public PlayerAttackState(PlayerAttackConfig config, IPlayerAttackHitbox hitbox)
    {
        _config = config;
        _hitbox = hitbox;
    }

    public void Enter(ActorContext context)
    {
        _elapsed = 0d;
        context.IsAttacking = true;
        context.AttackFinishedThisFrame = false;
        context.CanMove = false;
        context.AttackFacing = context.Intent.HasMoveInput ? context.Intent.Move.Normalized() : context.Facing;
        if (context.AttackFacing != Vector2.Zero)
        {
            context.Facing = context.AttackFacing;
        }

        _hitbox.Configure(context.AttackFacing, _config.AttackRange);
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
        var active = _elapsed >= _config.HitboxStartTime && _elapsed <= _config.HitboxEndTime;
        _hitbox.SetActive(active);

        if (_elapsed >= _config.TotalDuration)
        {
            _hitbox.SetActive(false);
            context.IsAttacking = false;
            context.CanMove = true;
            context.AttackFinishedThisFrame = true;
        }
    }
}
```

- [ ] **Step 3: Update the tests to use the final constructor and namespaces if needed**

```csharp
var state = new PlayerAttackState(config, hitbox);
state.Enter(context);
```

- [ ] **Step 4: Run the attack-state tests and verify they pass**

Run: `dotnet test /workspace/Tests/GodotGameTemplate.Tests.csproj --filter FullyQualifiedName~PlayerAttackStateTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add /workspace/Game/Gameplay/Player/States/PlayerAttackState.cs /workspace/Tests/Gameplay/Player/PlayerAttackStateTests.cs
git commit -m "feat: add player attack state"
```

### Task 5: Build The Runtime Hitbox And Training Dummy

**Files:**
- Create: `Game/Gameplay/Player/PlayerAttackHitbox.cs`
- Create: `Game/Gameplay/TrainingDummy/TrainingDummyController.cs`

- [ ] **Step 1: Implement the Godot-facing player hitbox**

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;
using GodotGameTemplate.Gameplay.Player.States;

namespace GodotGameTemplate.Gameplay.Player;

public partial class PlayerAttackHitbox : Area2D, IPlayerAttackHitbox
{
    [Export] public CollisionShape2D? CollisionShape { get; set; }
    [Export] public Node? OwnerNode { get; set; }
    [Export] public string AttackId { get; set; } = "player_basic_slash";

    private Vector2 _facing = Vector2.Down;
    private float _range = 18f;
    private readonly HashSet<Node> _hitTargets = [];

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;
        Monitoring = false;
        Monitorable = false;
    }

    public void Configure(Vector2 facing, float range)
    {
        _facing = facing == Vector2.Zero ? Vector2.Down : facing.Normalized();
        _range = range;
        Position = _facing * _range;
    }

    public void SetActive(bool active)
    {
        Monitoring = active;
        Monitorable = active;
        Visible = active;
    }

    public void ResetHitTargets()
    {
        _hitTargets.Clear();
    }

    private void OnBodyEntered(Node2D body) => TryHit(body);

    private void OnAreaEntered(Area2D area) => TryHit(area);

    private void TryHit(Node node)
    {
        if (!_hitTargets.Add(node))
        {
            return;
        }

        if (node is IHitReceiver receiver && OwnerNode is Node ownerNode)
        {
            receiver.ReceiveHit(new HitContext(ownerNode, _facing, AttackId));
        }
    }
}
```

- [ ] **Step 2: Implement the training dummy feedback target**

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;

namespace GodotGameTemplate.Gameplay.TrainingDummy;

public partial class TrainingDummyController : Node2D, IHitReceiver
{
    [Export] public Polygon2D? Body { get; set; }
    [Export] public float FlashDuration { get; set; } = 0.12f;

    private Color _defaultColor = Colors.White;
    private double _flashRemaining;

    public HitContext? LastHit { get; private set; }

    public override void _Ready()
    {
        if (Body != null)
        {
            _defaultColor = Body.Color;
        }
    }

    public override void _Process(double delta)
    {
        if (_flashRemaining <= 0d || Body == null)
        {
            return;
        }

        _flashRemaining -= delta;
        if (_flashRemaining <= 0d)
        {
            Body.Color = _defaultColor;
        }
    }

    public void ReceiveHit(HitContext hit)
    {
        LastHit = hit;
        _flashRemaining = FlashDuration;
        if (Body != null)
        {
            Body.Color = Colors.OrangeRed;
        }
    }
}
```

- [ ] **Step 3: If useful, add a targeted test for hitbox de-duplication later; for now keep runtime logic focused**

```csharp
// No new automated test in this step.
// The de-duplication behavior is covered by runtime hit target caching.
```

- [ ] **Step 4: Run the existing pure logic tests to keep the suite green**

Run: `dotnet test /workspace/Tests/GodotGameTemplate.Tests.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add /workspace/Game/Gameplay/Player/PlayerAttackHitbox.cs /workspace/Game/Gameplay/TrainingDummy/TrainingDummyController.cs
git commit -m "feat: add melee hitbox and training dummy"
```

### Task 6: Wire Attack State Into The Player Controller

**Files:**
- Modify: `Game/Gameplay/Player/PlayerController.cs`

- [ ] **Step 1: Add attack config and hitbox exports to the controller**

```csharp
[Export] public PlayerAttackConfig? AttackConfig { get; set; }
[Export] public PlayerAttackHitbox? AttackHitbox { get; set; }
```

- [ ] **Step 2: Instantiate the attack state and update state transitions**

```csharp
private PlayerAttackState _attackState = default!;

public override void _Ready()
{
    Config ??= new PlayerConfig();
    AttackConfig ??= new PlayerAttackConfig();
    View ??= GetNodeOrNull<PlayerView>("View");
    AttackHitbox ??= GetNodeOrNull<PlayerAttackHitbox>("AttackHitbox");

    _stateMachine = new StateMachine<ActorContext>(_context);
    _attackState = new PlayerAttackState(AttackConfig, AttackHitbox!);
    _stateMachine.ChangeState(_idleState);
    View?.Sync(_context);
}
```

- [ ] **Step 3: Update `_PhysicsProcess` to prioritize attack start, suppress movement during attack, and exit attack cleanly**

```csharp
public override void _PhysicsProcess(double delta)
{
    _context.AttackFinishedThisFrame = false;
    _context.Intent = _input.GetIntent();

    if (!_context.IsAttacking && _context.CanAttack && _context.Intent.AttackPressed)
    {
        _stateMachine.ChangeState(_attackState);
    }

    var moveInput = _context.CanMove ? _context.Intent.Move : Vector2.Zero;
    _context.Velocity = ActorMotor.UpdateVelocity(
        currentVelocity: Velocity,
        moveInput: moveInput,
        maxSpeed: Config!.MoveSpeed,
        acceleration: Config.Acceleration,
        deceleration: Config.Deceleration,
        delta: (float)delta
    );

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
```

- [ ] **Step 4: Run the pure tests again to guard against regressions**

Run: `dotnet test /workspace/Tests/GodotGameTemplate.Tests.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add /workspace/Game/Gameplay/Player/PlayerController.cs
git commit -m "feat: wire attack state into player controller"
```

### Task 7: Wire The Godot Scenes For Attack And Dummy Validation

**Files:**
- Modify: `Game/Scenes/Player/Player.tscn`
- Modify: `Game/Scenes/Main/Main.tscn`

- [ ] **Step 1: Add the hitbox node under the player scene**

```tscn
[ext_resource type="Script" path="res://Game/Gameplay/Player/PlayerAttackHitbox.cs" id="3_hitbox"]

[sub_resource type="RectangleShape2D" id="RectangleShape2D_hit"]
size = Vector2(18, 14)

[node name="AttackHitbox" type="Area2D" parent="."]
script = ExtResource("3_hitbox")

[node name="CollisionShape2D" type="CollisionShape2D" parent="AttackHitbox"]
shape = SubResource("RectangleShape2D_hit")
```

- [ ] **Step 2: Add a training dummy to the main scene**

```tscn
[ext_resource type="Script" path="res://Game/Gameplay/TrainingDummy/TrainingDummyController.cs" id="2_dummy"]

[node name="TrainingDummy" type="Node2D" parent="."]
position = Vector2(220, 90)
script = ExtResource("2_dummy")

[node name="Body" type="Polygon2D" parent="TrainingDummy"]
polygon = PackedVector2Array(-8, -12, 8, -12, 8, 12, -8, 12)
color = Color(0.968627, 0.760784, 0.360784, 1)
```

- [ ] **Step 3: Add a collision object to the dummy so the attack area can detect it**

```tscn
[sub_resource type="RectangleShape2D" id="RectangleShape2D_dummy"]
size = Vector2(16, 24)

[node name="Hurtbox" type="Area2D" parent="TrainingDummy"]

[node name="CollisionShape2D" type="CollisionShape2D" parent="TrainingDummy/Hurtbox"]
shape = SubResource("RectangleShape2D_dummy")
```

- [ ] **Step 4: Bind the dummy body in `_Ready()` if it is not exported in the scene**

```csharp
public override void _Ready()
{
    Body ??= GetNodeOrNull<Polygon2D>("Body");
    if (Body != null)
    {
        _defaultColor = Body.Color;
    }
}
```

- [ ] **Step 5: Run the project locally to manually verify the combat loop**

Run: `godot4 --path /workspace`
Expected: pressing the attack key near the dummy flashes the dummy and movement resumes after the attack

- [ ] **Step 6: Commit**

```bash
git add /workspace/Game/Scenes/Player/Player.tscn /workspace/Game/Scenes/Main/Main.tscn /workspace/Game/Gameplay/TrainingDummy/TrainingDummyController.cs
git commit -m "feat: add melee attack validation scene"
```

### Task 8: Update Documentation For The Combat Slice

**Files:**
- Modify: `Docs/Modules/player-controller.md`
- Create: `Docs/Modules/combat-basics.md`

- [ ] **Step 1: Add an attack section to the player controller doc**

```md
## 攻击链路

玩家攻击继续复用同一条控制主线：

`输入 -> ActorIntent -> PlayerAttackState -> PlayerAttackHitbox -> IHitReceiver`

其中：

- `PlayerInputAdapter` 负责产生攻击请求
- `PlayerAttackState` 负责锁定朝向、计时和命中窗口
- `PlayerAttackHitbox` 负责场景中的命中检测
- `IHitReceiver` 负责把目标反馈与玩家攻击逻辑解耦
```

- [ ] **Step 2: Write the dedicated combat module doc**

```md
# Combat Basics

当前战斗模块只实现首个最小闭环：

- 玩家普通近战攻击
- 短时 Hitbox 命中判定
- 通用命中接收接口
- 训练假人反馈

## 责任边界

- `PlayerAttackState`：管理攻击生命周期
- `PlayerAttackHitbox`：负责命中检测与命中去重
- `IHitReceiver`：定义目标如何接收一次命中
- `TrainingDummyController`：验证命中反馈链路
```

- [ ] **Step 3: Check docs for naming consistency**

Run: `grep -R "PlayerIntent\\|IHittable" /workspace/Docs/Modules`
Expected: no matches

- [ ] **Step 4: Commit**

```bash
git add /workspace/Docs/Modules/player-controller.md /workspace/Docs/Modules/combat-basics.md
git commit -m "docs: add melee combat module documentation"
```

### Task 9: Final Verification And Cleanup

**Files:**
- Modify as needed: `Game/Gameplay/Player/PlayerController.cs`
- Modify as needed: `Game/Gameplay/Player/States/PlayerAttackState.cs`
- Modify as needed: `Game/Gameplay/TrainingDummy/TrainingDummyController.cs`

- [ ] **Step 1: Run the full test suite**

Run: `dotnet test /workspace/Tests/GodotGameTemplate.Tests.csproj`
Expected: PASS

- [ ] **Step 2: Run a final headless Godot check**

Run: `godot4 --headless --path /workspace --quit`
Expected: PASS without scene load or script compile errors

- [ ] **Step 3: Run the formatter for the edited C# files**

Run: `dotnet csharpier format /workspace`
Expected: completes successfully or reports no changes

- [ ] **Step 4: Manually verify the gameplay checklist**

Checklist:

- attack input triggers one melee attack
- attack direction locks on state entry
- movement is blocked during the attack
- hitbox activates only inside the configured window
- dummy flashes when hit
- dummy is not hit outside the attack range
- docs describe the final file names and combat flow

- [ ] **Step 5: Commit**

```bash
git add /workspace
git commit -m "feat: add basic melee combat slice"
```

## Self-Review

### Spec Coverage

- Attack input added to the existing intent flow: covered by Task 1.
- Dedicated attack state with locked facing and timing: covered by Tasks 2, 3, 4, and 6.
- Short-lived hitbox and reusable hit receiver contract: covered by Tasks 3 and 5.
- Training dummy validation target: covered by Task 5 and scene wiring in Task 7.
- Docs for the combat slice: covered by Task 8.

### Placeholder Scan

- No `TODO`, `TBD`, or “implement later” placeholders remain.
- Each task includes concrete code or commands.
- Manual checks are explicit and tied to this slice only.

### Type Consistency

- `ActorIntent` gains `AttackPressed` consistently across Tasks 1, 2, and 6.
- `ActorContext` gains `AttackFacing`, `IsAttacking`, and `AttackFinishedThisFrame` consistently across Tasks 3, 4, and 6.
- `PlayerAttackState`, `IPlayerAttackHitbox`, `HitContext`, and `IHitReceiver` use one stable naming scheme throughout.
