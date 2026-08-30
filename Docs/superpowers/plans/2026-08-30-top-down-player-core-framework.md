# Top-Down Player Core Framework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a commercial-quality, reusable player control foundation for a top-down 2D pixel action game in Godot 4 + C#, centered on eight-direction continuous movement and a decoupled `input -> intent -> logic -> presentation` pipeline.

**Architecture:** Use Godot scenes and nodes for runtime composition, rendering, input reading, and movement application, while keeping the control rules in focused C# modules. Generalize core control types around `Actor` instead of `Player` so the state machine and intent model can later be reused by enemies or NPCs without rewriting the foundations.

**Tech Stack:** Godot 4.2, C#, `CharacterBody2D`, Godot `Resource` configuration, lightweight self-authored FSM, focused automated tests for pure logic, Markdown docs under `Docs/`

---

## Summary

This implementation creates the first playable framework slice, not a full game. The deliverable is a controllable player character in a minimal main scene with:

- eight-direction continuous movement
- input abstraction that is ready for keyboard, controller, and future mobile virtual controls
- a reusable actor intent model
- a lightweight reusable FSM
- a clean split between gameplay logic and presentation hooks
- concise architecture and module documentation

## Current State Analysis

- The repository is currently a Godot 4 + C# template with only structural folders and `project.godot`.
- There is no existing gameplay code, no player scene, no test project, and no gameplay documentation beyond folder placeholders.
- The existing project structure already reserves useful roots: `Game/Gameplay/`, `Game/Scenes/`, `Game/Config/`, `Docs/`, and `Tests/`.
- `project.godot` declares Godot `4.2` and C# support, so the plan should stay aligned with that stack and avoid introducing unrelated frameworks.

## Assumptions & Decisions

- The first milestone covers only the player control framework, not combat, enemies, saves, or progression systems.
- Movement is continuous, not grid-based.
- The project is PC-first, but the input architecture must remain ready for gamepad and mobile virtual controls.
- The core design uses `input -> intent -> logic -> presentation`.
- FSM is implemented in-house as a small reusable utility, not via a third-party package.
- Reuse is targeted at the module level inside Godot projects; no separate engine-agnostic NuGet package is introduced in this milestone.
- The first state set is intentionally small: `Idle` and `Move`.
- Documentation should be short and clear, focused on architecture and module responsibilities.

## File Structure Plan

### New Runtime Files

- Create: `Game/Gameplay/Actors/ActorIntent.cs`
- Create: `Game/Gameplay/Actors/ActorContext.cs`
- Create: `Game/Gameplay/Actors/ActorMotor.cs`
- Create: `Game/Gameplay/Common/StateMachine/IState.cs`
- Create: `Game/Gameplay/Common/StateMachine/StateMachine.cs`
- Create: `Game/Gameplay/Input/IIntentProvider.cs`
- Create: `Game/Gameplay/Input/PlayerInputAdapter.cs`
- Create: `Game/Gameplay/Player/PlayerController.cs`
- Create: `Game/Gameplay/Player/PlayerView.cs`
- Create: `Game/Gameplay/Player/States/PlayerIdleState.cs`
- Create: `Game/Gameplay/Player/States/PlayerMoveState.cs`
- Create: `Game/Config/Player/PlayerConfig.cs`
- Create: `Game/Scenes/Player/Player.tscn`
- Create: `Game/Scenes/Main/Main.tscn`

### New Tests

- Create: `Tests/Gameplay/Actors/ActorMotorTests.cs`
- Create: `Tests/Gameplay/Common/StateMachineTests.cs`
- Create: `Tests/README.md` update to explain how pure-logic tests are run

### New Docs

- Create: `Docs/Architecture/overall-architecture.md`
- Create: `Docs/Modules/player-controller.md`
- Create: `Docs/Modules/state-machine.md`

### Existing Files To Modify

- Modify: `README.md`
- Modify: `project.godot`

## Proposed Changes

### `Game/Gameplay/Actors/ActorIntent.cs`

- Define a small immutable or readonly-friendly data model for control intent.
- Include normalized move direction and future-proof placeholders only where they support immediate extensibility, such as simple booleans for action triggers if needed by interfaces.
- Keep it independent of Godot input APIs so it can be produced by player input or AI later.

### `Game/Gameplay/Actors/ActorContext.cs`

- Hold runtime state required by FSM and motor, such as current input intent, facing direction, desired velocity, and movement permission flags.
- Avoid hard references to rendering nodes or input devices.

### `Game/Gameplay/Actors/ActorMotor.cs`

- Compute target velocity from movement intent and config values.
- Handle acceleration, deceleration, and directional stop/start behavior in one place.
- Expose deterministic methods that can be unit-tested without a scene.

### `Game/Gameplay/Common/StateMachine/*`

- Provide a minimal reusable FSM with:
  - current state
  - transition API
  - enter/exit callbacks
  - frame update
  - physics update
- Keep generic naming so it can be reused by player and future enemy actors.

### `Game/Gameplay/Input/*`

- `IIntentProvider` defines the shape that produces `ActorIntent`.
- `PlayerInputAdapter` reads Godot input actions and emits `ActorIntent`.
- All device-specific reads stop here; gameplay modules consume intent only.

### `Game/Gameplay/Player/*`

- `PlayerController` is the scene-facing orchestrator.
- It wires together config, input adapter, actor context, FSM, motor, and view.
- `PlayerView` translates logic output into presentation-facing data for sprite/animation direction and later animation tree parameters.
- `PlayerIdleState` and `PlayerMoveState` implement the first concrete states using the shared FSM.

### `Game/Config/Player/PlayerConfig.cs`

- Create a Godot `Resource` that stores move speed, acceleration, and deceleration.
- Keep tuning values out of gameplay classes.

### `Game/Scenes/*`

- `Player.tscn` becomes the minimal player scene with a `CharacterBody2D` root and presentation-ready child nodes.
- `Main.tscn` becomes the minimal sandbox scene for validating movement.

### `Docs/*`

- `overall-architecture.md` explains layers, dependency direction, directory rules, and reuse goals.
- `player-controller.md` explains the full player control pipeline.
- `state-machine.md` explains the shared FSM API, intended use, and non-goals.

### `README.md`

- Add a concise project-specific section that points to architecture docs and explains what the first framework slice contains.

### `project.godot`

- Update project name and any input map entries required by the player control framework.

## Verification Strategy

### Automated

- Unit-test `ActorMotor` with focused cases:
  - no input produces stopped or decelerating output
  - diagonal input is normalized
  - acceleration approaches configured speed
- Unit-test `StateMachine` with focused cases:
  - transition triggers exit/enter in the right order
  - update dispatch reaches the current state only

### Manual

- Launch the main scene.
- Verify eight-direction movement with keyboard.
- Verify movement starts and stops smoothly according to config.
- Verify diagonal movement is not faster than cardinal movement.
- Verify `Idle` and `Move` states switch as expected.
- Verify facing/presentation output updates when direction changes.

## Task Breakdown

### Task 1: Establish Input Actions And Core File Skeleton

**Files:**
- Create: `Game/Gameplay/Actors/ActorIntent.cs`
- Create: `Game/Gameplay/Input/IIntentProvider.cs`
- Modify: `project.godot`
- Modify: `README.md`

- [ ] **Step 1: Add input action definitions to the Godot project**

```ini
[input]

move_left={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":65), Object(InputEventKey,"physical_keycode":16777231)]
}
move_right={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":68), Object(InputEventKey,"physical_keycode":16777233)]
}
move_up={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":87), Object(InputEventKey,"physical_keycode":16777232)]
}
move_down={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":83), Object(InputEventKey,"physical_keycode":16777234)]
}
```

- [ ] **Step 2: Define the reusable actor intent model**

```csharp
namespace GodotGameTemplate.Gameplay.Actors;

public readonly record struct ActorIntent(Vector2 Move)
{
    public bool HasMoveInput => Move.LengthSquared() > 0f;

    public static ActorIntent None => new(Vector2.Zero);
}
```

- [ ] **Step 3: Define the intent provider interface**

```csharp
using GodotGameTemplate.Gameplay.Actors;

namespace GodotGameTemplate.Gameplay.Input;

public interface IIntentProvider
{
    ActorIntent GetIntent();
}
```

- [ ] **Step 4: Update the root README with framework slice scope**

```md
## First Gameplay Slice

This repository now hosts the first reusable gameplay foundation for a top-down 2D pixel action game:

- eight-direction player movement
- decoupled input-intent-logic-presentation flow
- lightweight reusable FSM
- concise architecture docs in `Docs/`
```

- [ ] **Step 5: Run the Godot project once to validate input map syntax**

Run: `godot4 --headless --path /workspace --quit`
Expected: exits successfully without project configuration errors

- [ ] **Step 6: Commit**

```bash
git add /workspace/project.godot /workspace/README.md /workspace/Game/Gameplay/Actors/ActorIntent.cs /workspace/Game/Gameplay/Input/IIntentProvider.cs
git commit -m "feat: add actor intent foundation"
```

### Task 2: Add Focused Logic Tests Before Implementing Motor And FSM

**Files:**
- Create: `Tests/Gameplay/Actors/ActorMotorTests.cs`
- Create: `Tests/Gameplay/Common/StateMachineTests.cs`
- Modify: `Tests/README.md`

- [ ] **Step 1: Write the failing motor tests**

```csharp
using Xunit;

public class ActorMotorTests
{
    [Fact]
    public void UpdateVelocity_NoInput_DeceleratesToZero()
    {
        var velocity = new Vector2(100f, 0f);

        var next = ActorMotor.UpdateVelocity(
            currentVelocity: velocity,
            moveInput: Vector2.Zero,
            maxSpeed: 120f,
            acceleration: 600f,
            deceleration: 800f,
            delta: 0.1f);

        Assert.True(next.X < velocity.X);
        Assert.True(next.Y == 0f);
    }

    [Fact]
    public void UpdateVelocity_DiagonalInput_IsNormalized()
    {
        var next = ActorMotor.UpdateVelocity(
            currentVelocity: Vector2.Zero,
            moveInput: new Vector2(1f, 1f),
            maxSpeed: 120f,
            acceleration: 1200f,
            deceleration: 800f,
            delta: 1f);

        Assert.True(next.Length() <= 120.01f);
    }
}
```

- [ ] **Step 2: Write the failing FSM tests**

```csharp
using Xunit;

public class StateMachineTests
{
    [Fact]
    public void ChangeState_CallsExitThenEnter()
    {
        var log = new List<string>();
        var machine = new StateMachine<string>();
        var idle = new FakeState("Idle", log);
        var move = new FakeState("Move", log);

        machine.ChangeState(idle);
        machine.ChangeState(move);

        Assert.Equal(new[] { "Idle:Enter", "Idle:Exit", "Move:Enter" }, log);
    }
}
```

- [ ] **Step 3: Document how the pure-logic tests are intended to run**

```md
## Gameplay Tests

The first automated tests cover pure control logic that does not require a running scene:

- actor motor rules
- lightweight FSM behavior
```

- [ ] **Step 4: Run the targeted tests to confirm they fail**

Run: `dotnet test /workspace --filter "FullyQualifiedName~ActorMotorTests|FullyQualifiedName~StateMachineTests"`
Expected: FAIL because `ActorMotor` and `StateMachine` do not exist yet

- [ ] **Step 5: Commit**

```bash
git add /workspace/Tests
git commit -m "test: add failing control logic tests"
```

### Task 3: Implement The Reusable Actor Motor

**Files:**
- Create: `Game/Gameplay/Actors/ActorMotor.cs`
- Create: `Game/Config/Player/PlayerConfig.cs`
- Modify: `Tests/Gameplay/Actors/ActorMotorTests.cs`

- [ ] **Step 1: Implement the motor with deterministic static logic**

```csharp
namespace GodotGameTemplate.Gameplay.Actors;

public static class ActorMotor
{
    public static Vector2 UpdateVelocity(
        Vector2 currentVelocity,
        Vector2 moveInput,
        float maxSpeed,
        float acceleration,
        float deceleration,
        float delta)
    {
        var direction = moveInput.LengthSquared() > 1f
            ? moveInput.Normalized()
            : moveInput;

        var targetVelocity = direction * maxSpeed;
        var rate = direction == Vector2.Zero ? deceleration : acceleration;

        return currentVelocity.MoveToward(targetVelocity, rate * delta);
    }
}
```

- [ ] **Step 2: Add player movement config as a Resource**

```csharp
using Godot;

namespace GodotGameTemplate.Config.Player;

[GlobalClass]
public partial class PlayerConfig : Resource
{
    [Export] public float MoveSpeed { get; set; } = 120f;
    [Export] public float Acceleration { get; set; } = 900f;
    [Export] public float Deceleration { get; set; } = 1100f;
}
```

- [ ] **Step 3: Update tests if needed to match the final method signature**

```csharp
var next = ActorMotor.UpdateVelocity(
    currentVelocity: Vector2.Zero,
    moveInput: new Vector2(1f, 1f),
    maxSpeed: 120f,
    acceleration: 1200f,
    deceleration: 800f,
    delta: 1f);
```

- [ ] **Step 4: Run motor tests and verify they pass**

Run: `dotnet test /workspace --filter FullyQualifiedName~ActorMotorTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add /workspace/Game/Gameplay/Actors/ActorMotor.cs /workspace/Game/Config/Player/PlayerConfig.cs /workspace/Tests/Gameplay/Actors/ActorMotorTests.cs
git commit -m "feat: add reusable actor motor"
```

### Task 4: Implement The Reusable Lightweight FSM

**Files:**
- Create: `Game/Gameplay/Common/StateMachine/IState.cs`
- Create: `Game/Gameplay/Common/StateMachine/StateMachine.cs`
- Modify: `Tests/Gameplay/Common/StateMachineTests.cs`

- [ ] **Step 1: Define the state interface**

```csharp
namespace GodotGameTemplate.Gameplay.Common.StateMachine;

public interface IState<TContext>
{
    void Enter(TContext context);
    void Exit(TContext context);
    void Update(TContext context, double delta);
    void PhysicsUpdate(TContext context, double delta);
}
```

- [ ] **Step 2: Implement the minimal state machine**

```csharp
namespace GodotGameTemplate.Gameplay.Common.StateMachine;

public sealed class StateMachine<TContext>
{
    private readonly TContext _context;

    public StateMachine(TContext context)
    {
        _context = context;
    }

    public IState<TContext>? CurrentState { get; private set; }

    public void ChangeState(IState<TContext> next)
    {
        CurrentState?.Exit(_context);
        CurrentState = next;
        CurrentState.Enter(_context);
    }

    public void Update(double delta) => CurrentState?.Update(_context, delta);

    public void PhysicsUpdate(double delta) => CurrentState?.PhysicsUpdate(_context, delta);
}
```

- [ ] **Step 3: Align the tests with the generic context API**

```csharp
var context = new object();
var machine = new StateMachine<object>(context);
```

- [ ] **Step 4: Run FSM tests and verify they pass**

Run: `dotnet test /workspace --filter FullyQualifiedName~StateMachineTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add /workspace/Game/Gameplay/Common/StateMachine /workspace/Tests/Gameplay/Common/StateMachineTests.cs
git commit -m "feat: add lightweight reusable state machine"
```

### Task 5: Build The Player Control Runtime Chain

**Files:**
- Create: `Game/Gameplay/Actors/ActorContext.cs`
- Create: `Game/Gameplay/Input/PlayerInputAdapter.cs`
- Create: `Game/Gameplay/Player/PlayerController.cs`
- Create: `Game/Gameplay/Player/PlayerView.cs`
- Create: `Game/Gameplay/Player/States/PlayerIdleState.cs`
- Create: `Game/Gameplay/Player/States/PlayerMoveState.cs`

- [ ] **Step 1: Define the actor context**

```csharp
namespace GodotGameTemplate.Gameplay.Actors;

public sealed class ActorContext
{
    public ActorIntent Intent { get; set; } = ActorIntent.None;
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public Vector2 Facing { get; set; } = Vector2.Down;

    public bool HasMoveInput => Intent.HasMoveInput;
}
```

- [ ] **Step 2: Implement the Godot-facing input adapter**

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Actors;

namespace GodotGameTemplate.Gameplay.Input;

public sealed class PlayerInputAdapter : IIntentProvider
{
    public ActorIntent GetIntent()
    {
        var move = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        return new ActorIntent(move);
    }
}
```

- [ ] **Step 3: Implement the first player states**

```csharp
public sealed class PlayerIdleState : IState<ActorContext>
{
    public void Enter(ActorContext context) { }
    public void Exit(ActorContext context) { }
    public void Update(ActorContext context, double delta) { }

    public void PhysicsUpdate(ActorContext context, double delta)
    {
        if (context.HasMoveInput)
        {
            context.Facing = context.Intent.Move.Normalized();
        }
    }
}
```

```csharp
public sealed class PlayerMoveState : IState<ActorContext>
{
    public void Enter(ActorContext context) { }
    public void Exit(ActorContext context) { }
    public void Update(ActorContext context, double delta) { }

    public void PhysicsUpdate(ActorContext context, double delta)
    {
        if (context.HasMoveInput)
        {
            context.Facing = context.Intent.Move.Normalized();
        }
    }
}
```

- [ ] **Step 4: Implement the player view bridge**

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Actors;

namespace GodotGameTemplate.Gameplay.Player;

public partial class PlayerView : Node
{
    public void Sync(ActorContext context)
    {
        // Initial slice: keep this as the single presentation sync point.
        Rotation = context.Facing.Angle();
    }
}
```

- [ ] **Step 5: Implement the player controller orchestrator**

```csharp
using Godot;
using GodotGameTemplate.Config.Player;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;
using GodotGameTemplate.Gameplay.Input;
using GodotGameTemplate.Gameplay.Player.States;

namespace GodotGameTemplate.Gameplay.Player;

public partial class PlayerController : CharacterBody2D
{
    [Export] public PlayerConfig Config { get; set; } = default!;
    [Export] public PlayerView View { get; set; } = default!;

    private readonly ActorContext _context = new();
    private readonly PlayerInputAdapter _input = new();
    private StateMachine<ActorContext> _stateMachine = default!;
    private readonly PlayerIdleState _idle = new();
    private readonly PlayerMoveState _move = new();

    public override void _Ready()
    {
        _stateMachine = new StateMachine<ActorContext>(_context);
        _stateMachine.ChangeState(_idle);
    }

    public override void _PhysicsProcess(double delta)
    {
        _context.Intent = _input.GetIntent();
        _context.Velocity = ActorMotor.UpdateVelocity(
            Velocity,
            _context.Intent.Move,
            Config.MoveSpeed,
            Config.Acceleration,
            Config.Deceleration,
            (float)delta);

        _stateMachine.ChangeState(_context.HasMoveInput ? _move : _idle);
        Velocity = _context.Velocity;
        MoveAndSlide();
        View.Sync(_context);
    }
}
```

- [ ] **Step 6: Run the project and confirm the player script loads without compile errors**

Run: `godot4 --headless --path /workspace --quit`
Expected: exits successfully and compiles scripts

- [ ] **Step 7: Commit**

```bash
git add /workspace/Game/Gameplay/Actors/ActorContext.cs /workspace/Game/Gameplay/Input/PlayerInputAdapter.cs /workspace/Game/Gameplay/Player
git commit -m "feat: add player control runtime chain"
```

### Task 6: Create Minimal Scenes And Hook Up Runtime Objects

**Files:**
- Create: `Game/Scenes/Player/Player.tscn`
- Create: `Game/Scenes/Main/Main.tscn`
- Modify: `project.godot`

- [ ] **Step 1: Create the player scene with exported references wired**

```tscn
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" path="res://Game/Gameplay/Player/PlayerController.cs" id="1"]
[ext_resource type="Script" path="res://Game/Gameplay/Player/PlayerView.cs" id="2"]

[node name="Player" type="CharacterBody2D"]
script = ExtResource("1")

[node name="View" type="Node2D" parent="."]
script = ExtResource("2")
```

- [ ] **Step 2: Create the main sandbox scene**

```tscn
[gd_scene load_steps=2 format=3]

[ext_resource type="PackedScene" path="res://Game/Scenes/Player/Player.tscn" id="1"]

[node name="Main" type="Node2D"]

[node name="Player" parent="." instance=ExtResource("1")]
position = Vector2(160, 90)
```

- [ ] **Step 3: Set the main scene in project settings**

```ini
[application]
run/main_scene="res://Game/Scenes/Main/Main.tscn"
```

- [ ] **Step 4: Run the project and manually validate movement**

Run: `godot4 --path /workspace`
Expected: the player character starts in `Main.tscn` and responds to eight-direction input

- [ ] **Step 5: Commit**

```bash
git add /workspace/Game/Scenes /workspace/project.godot
git commit -m "feat: add playable player control scenes"
```

### Task 7: Write The Architecture And Module Docs

**Files:**
- Create: `Docs/Architecture/overall-architecture.md`
- Create: `Docs/Modules/player-controller.md`
- Create: `Docs/Modules/state-machine.md`

- [ ] **Step 1: Document the overall architecture**

```md
# Overall Architecture

## Layers

- Input adapter: reads Godot input devices
- Intent: expresses actor control intent
- Logic: updates state and movement
- Presentation: reflects logic into visible output

## Dependency Direction

`Input -> Intent -> Logic -> Presentation`
```

- [ ] **Step 2: Document the player controller module**

```md
# Player Controller

`PlayerController` is the Godot-facing coordinator for the player actor.

It:

- reads intent from `PlayerInputAdapter`
- updates `ActorContext`
- computes velocity through `ActorMotor`
- switches between player states
- forwards visible changes to `PlayerView`
```

- [ ] **Step 3: Document the FSM module**

```md
# State Machine

The FSM is a small reusable utility for actor behavior.

It supports:

- state enter
- state exit
- update
- physics update

It does not yet support hierarchical or parallel states.
```

- [ ] **Step 4: Review docs for consistency with the implemented class names**

Run: `grep -R "PlayerIntent\\|third-party" /workspace/Docs`
Expected: no references to removed naming or rejected FSM dependency choices

- [ ] **Step 5: Commit**

```bash
git add /workspace/Docs/Architecture /workspace/Docs/Modules
git commit -m "docs: add core architecture documentation"
```

### Task 8: Final Verification And Cleanup

**Files:**
- Modify as needed: `Game/Gameplay/Player/PlayerController.cs`
- Modify as needed: `Docs/Modules/player-controller.md`
- Modify as needed: `README.md`

- [ ] **Step 1: Run the focused automated tests**

Run: `dotnet test /workspace`
Expected: PASS

- [ ] **Step 2: Run a final headless project check**

Run: `godot4 --headless --path /workspace --quit`
Expected: PASS without script compile or resource load errors

- [ ] **Step 3: Run a final manual gameplay pass**

Checklist:

- player spawns in the main scene
- keyboard movement works in 8 directions
- diagonal speed is capped
- stop/start feels responsive
- facing output changes with movement direction
- docs match the final class names and file locations

- [ ] **Step 4: If any recent file changes introduced C# formatting issues, run the project formatter or normalize the edited code**

Run: `dotnet format /workspace`
Expected: completes successfully or reports no changes

- [ ] **Step 5: Commit**

```bash
git add /workspace
git commit -m "chore: finalize player control framework slice"
```

## Self-Review

### Spec Coverage

- Eight-direction continuous movement: covered by Tasks 3, 5, 6, and 8.
- Input abstraction for future device reuse: covered by Tasks 1 and 5.
- Reusable FSM with self-authored implementation: covered by Task 4.
- Clear module decoupling and actor-level reuse: covered by Tasks 1, 3, 4, and 5.
- Concise documentation: covered by Task 7 and README updates.

### Placeholder Scan

- No `TODO`, `TBD`, or “implement later” placeholders remain in the plan.
- All code-bearing steps include concrete code or configuration snippets.
- All verification steps include exact commands or explicit manual checklists.

### Type Consistency

- `ActorIntent`, `ActorContext`, `ActorMotor`, `IIntentProvider`, `StateMachine<TContext>`, `PlayerController`, and `PlayerView` use consistent names throughout the plan.
- The plan intentionally replaces `PlayerIntent` with `ActorIntent` to preserve future actor reuse.

