# Skill System (Full) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make all 6 skill slots fully functional with Mana + cooldowns, including Secondary ground-targeting with an indicator and cross-platform confirm/cancel.

**Architecture:** Add a pure-logic skill layer (Mana/Cooldown/Orchestrator) and connect it to Godot via player states (`PlayerSkillCastState`, `PlayerAoeTargetingState`) and minimal effect nodes (Projectile/AOE). UI reads from player/session state to show Mana + skill bar.

**Tech Stack:** Godot 4.x (C#), xUnit tests, CSharpier formatting.

---

## File map

### Create (pure logic)
- `Game/Gameplay/Progression/ManaModel.cs`
- `Game/Gameplay/Skills/CooldownModel.cs`
- `Game/Gameplay/Skills/SkillDefinition.cs`
- `Game/Gameplay/Skills/SkillDatabase.cs`
- `Game/Gameplay/Skills/SkillOrchestrator.cs`
- `Game/Gameplay/Skills/SkillDecision.cs`

### Create (Godot glue)
- `Game/Gameplay/Player/States/PlayerSkillCastState.cs`
- `Game/Gameplay/Player/States/PlayerAoeTargetingState.cs`
- `Game/Scenes/Skills/AoeIndicator.tscn`
- `Game/Scenes/Skills/AoeIndicator.cs`
- `Game/Scenes/Skills/ProjectileSkillEffect.tscn`
- `Game/Scenes/Skills/ProjectileSkillEffect.cs`
- `Game/Scenes/Skills/AoeStrikeSkillEffect.tscn`
- `Game/Scenes/Skills/AoeStrikeSkillEffect.cs`

### Modify (integration)
- `Game/Gameplay/Actors/ActorContext.cs` (add Mana/Cooldowns + casting/targeting flags)
- `Game/Gameplay/Input/Commands/PlayerCommand.cs` (add confirm/cancel + aim vector)
- `Game/Gameplay/Input/MouseKeyboardInputAdapter.cs`
- `Game/Gameplay/Input/GamepadInputAdapter.cs`
- `Game/Gameplay/Input/TouchInputAdapter.cs`
- `Game/Gameplay/Player/PlayerController.cs` (consume skill decisions; state transitions; input consumption rules)
- `Game/Gameplay/Session/GameSession.cs` (add Mana + cooldown persistence in-session; optional save)
- `Game/UI/Hud/Hud.tscn` + `Game/UI/Hud/Hud.cs` (Mana + skill bar + reject toast)
- `Game/Config/GameFeatures.cs` (add `EnableSkills`)

### Tests
- `Tests/Gameplay/Progression/ManaModelTests.cs`
- `Tests/Gameplay/Skills/CooldownModelTests.cs`
- `Tests/Gameplay/Skills/SkillOrchestratorTests.cs`
- `Tests/Gameplay/Input/PlayerCommandTests.cs` (if needed for merge/edge semantics)

---

## Task 1: Add ManaModel (pure logic)

**Files:**
- Create: `Game/Gameplay/Progression/ManaModel.cs`
- Create: `Tests/Gameplay/Progression/ManaModelTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Tests/Gameplay/Progression/ManaModelTests.cs`:

```csharp
using GodotGameTemplate.Gameplay.Progression;

namespace GodotGameTemplate.Tests.Gameplay.Progression;

public sealed class ManaModelTests
{
    [Fact]
    public void TryConsume_WhenEnoughMana_ReturnsTrueAndReducesCurrent()
    {
        var mana = new ManaModel();
        var ok = mana.TryConsume(30f);
        Assert.True(ok);
        Assert.Equal(70f, mana.Current);
    }

    [Fact]
    public void TryConsume_WhenManaNotEnough_ReturnsFalseAndKeepsCurrent()
    {
        var mana = new ManaModel();
        var ok = mana.TryConsume(200f);
        Assert.False(ok);
        Assert.Equal(mana.Max, mana.Current);
    }

    [Fact]
    public void Tick_RegenClampsCurrentToMax()
    {
        var mana = new ManaModel();
        mana.TryConsume(50f);
        mana.Tick(10f);
        Assert.Equal(mana.Max, mana.Current);
    }
}
```

- [ ] **Step 2: Run tests (expect fail)**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj`
Expected: FAIL, cannot find `ManaModel`.

- [ ] **Step 3: Implement ManaModel**

Create `Game/Gameplay/Progression/ManaModel.cs`:

```csharp
using System;

namespace GodotGameTemplate.Gameplay.Progression;

/// <summary>
/// 法力模型（纯逻辑）：
/// <para>
/// - 维护当前法力（<see cref="Current"/>）与最大法力（<see cref="Max"/>）
/// - 提供按秒恢复（<see cref="Tick"/>）与消耗（<see cref="TryConsume"/>）
/// </para>
/// </summary>
public sealed class ManaModel
{
    public float Max { get; set; } = 100f;

    public float Current { get; private set; } = 100f;

    public float RegenPerSecond { get; set; } = 12f;

    public void ResetFull()
    {
        Current = Max;
    }

    public void Tick(float delta)
    {
        if (delta <= 0f)
        {
            return;
        }

        Current = MathF.Min(Max, Current + RegenPerSecond * delta);
    }

    public bool TryConsume(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (Current < amount)
        {
            return false;
        }

        Current -= amount;
        return true;
    }
}
```

- [ ] **Step 4: Run tests (expect pass)**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add Tests/Gameplay/Progression/ManaModelTests.cs Game/Gameplay/Progression/ManaModel.cs
git commit -m "feat(skills): add mana model"
```

---

## Task 2: Add CooldownModel (pure logic)

**Files:**
- Create: `Game/Gameplay/Skills/CooldownModel.cs`
- Create: `Tests/Gameplay/Skills/CooldownModelTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Tests/Gameplay/Skills/CooldownModelTests.cs`:

```csharp
using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Tests.Gameplay.Skills;

public sealed class CooldownModelTests
{
    [Fact]
    public void IsReady_Default_IsTrue()
    {
        var cd = new CooldownModel();
        Assert.True(cd.IsReady(SkillSlot.Skill1));
        Assert.Equal(0f, cd.GetRemaining(SkillSlot.Skill1));
    }

    [Fact]
    public void Start_SetsRemaining()
    {
        var cd = new CooldownModel();
        cd.Start(SkillSlot.Skill1, 1.5f);
        Assert.False(cd.IsReady(SkillSlot.Skill1));
        Assert.True(cd.GetRemaining(SkillSlot.Skill1) > 0f);
    }

    [Fact]
    public void Tick_DecreasesAndClampsToZero()
    {
        var cd = new CooldownModel();
        cd.Start(SkillSlot.Skill1, 1f);
        cd.Tick(0.4f);
        Assert.InRange(cd.GetRemaining(SkillSlot.Skill1), 0.59f, 0.61f);
        cd.Tick(10f);
        Assert.Equal(0f, cd.GetRemaining(SkillSlot.Skill1));
        Assert.True(cd.IsReady(SkillSlot.Skill1));
    }
}
```

- [ ] **Step 2: Run tests (expect fail)**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj`
Expected: FAIL, cannot find `CooldownModel`.

- [ ] **Step 3: Implement CooldownModel**

Create `Game/Gameplay/Skills/CooldownModel.cs`:

```csharp
using System;
using System.Collections.Generic;
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Skills;

/// <summary>
/// 冷却模型（纯逻辑）：按技能槽位维护剩余冷却时间（秒）。
/// </summary>
public sealed class CooldownModel
{
    private readonly Dictionary<SkillSlot, float> _remaining = new();

    public float GetRemaining(SkillSlot slot)
    {
        return _remaining.TryGetValue(slot, out var value) ? value : 0f;
    }

    public bool IsReady(SkillSlot slot)
    {
        return GetRemaining(slot) <= 0f;
    }

    public void Start(SkillSlot slot, float seconds)
    {
        _remaining[slot] = MathF.Max(0f, seconds);
    }

    public void Tick(float delta)
    {
        if (delta <= 0f || _remaining.Count == 0)
        {
            return;
        }

        // 避免边迭代边修改：先复制 key 列表
        var keys = _remaining.Keys.ToArray();
        foreach (var key in keys)
        {
            var next = MathF.Max(0f, _remaining[key] - delta);
            if (next <= 0f)
            {
                _remaining.Remove(key);
            }
            else
            {
                _remaining[key] = next;
            }
        }
    }
}
```

- [ ] **Step 4: Run tests (expect pass)**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add Tests/Gameplay/Skills/CooldownModelTests.cs Game/Gameplay/Skills/CooldownModel.cs
git commit -m "feat(skills): add cooldown model"
```

---

## Task 3: Define skills (definitions + database)

**Files:**
- Create: `Game/Gameplay/Skills/SkillDefinition.cs`
- Create: `Game/Gameplay/Skills/SkillDatabase.cs`

- [ ] **Step 1: Add skill definition types**

Create `Game/Gameplay/Skills/SkillDefinition.cs`:

```csharp
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Skills;

public enum SkillCastType
{
    Instant = 0,
    AoeTargeting = 1,
}

public enum SkillEffectKind
{
    None = 0,
    Projectile = 1,
    AoeStrike = 2,
}

public sealed record SkillDefinition(
    string SkillId,
    SkillSlot Slot,
    SkillCastType CastType,
    SkillEffectKind EffectKind,
    float CooldownSeconds,
    float ManaCost,
    float Range,
    float AoeRadius
);
```

- [ ] **Step 2: Add a minimal database**

Create `Game/Gameplay/Skills/SkillDatabase.cs`:

```csharp
using System.Collections.Generic;
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Skills;

/// <summary>
/// 技能表（首版先硬编码，后续再改成 Resource/表格）。
/// </summary>
public static class SkillDatabase
{
    public static IReadOnlyDictionary<SkillSlot, SkillDefinition> DefaultBySlot { get; } =
        new Dictionary<SkillSlot, SkillDefinition>
        {
            // Primary：沿用普攻（不在此处实现效果），仍可用于 UI 显示
            [SkillSlot.Primary] = new SkillDefinition(
                SkillId: "skill_primary_melee",
                Slot: SkillSlot.Primary,
                CastType: SkillCastType.Instant,
                EffectKind: SkillEffectKind.None,
                CooldownSeconds: 0f,
                ManaCost: 0f,
                Range: 18f,
                AoeRadius: 0f
            ),
            // Secondary：地面选点 AOE
            [SkillSlot.Secondary] = new SkillDefinition(
                SkillId: "skill_secondary_aoe",
                Slot: SkillSlot.Secondary,
                CastType: SkillCastType.AoeTargeting,
                EffectKind: SkillEffectKind.AoeStrike,
                CooldownSeconds: 4f,
                ManaCost: 25f,
                Range: 120f,
                AoeRadius: 26f
            ),
            [SkillSlot.Skill1] = new SkillDefinition(
                SkillId: "skill1_projectile",
                Slot: SkillSlot.Skill1,
                CastType: SkillCastType.Instant,
                EffectKind: SkillEffectKind.Projectile,
                CooldownSeconds: 1.5f,
                ManaCost: 10f,
                Range: 160f,
                AoeRadius: 0f
            ),
            [SkillSlot.Skill2] = new SkillDefinition(
                SkillId: "skill2_projectile",
                Slot: SkillSlot.Skill2,
                CastType: SkillCastType.Instant,
                EffectKind: SkillEffectKind.Projectile,
                CooldownSeconds: 2.5f,
                ManaCost: 18f,
                Range: 200f,
                AoeRadius: 0f
            ),
            [SkillSlot.Skill3] = new SkillDefinition(
                SkillId: "skill3_aoe_small",
                Slot: SkillSlot.Skill3,
                CastType: SkillCastType.Instant,
                EffectKind: SkillEffectKind.AoeStrike,
                CooldownSeconds: 3f,
                ManaCost: 15f,
                Range: 80f,
                AoeRadius: 16f
            ),
            [SkillSlot.Skill4] = new SkillDefinition(
                SkillId: "skill4_aoe_big",
                Slot: SkillSlot.Skill4,
                CastType: SkillCastType.Instant,
                EffectKind: SkillEffectKind.AoeStrike,
                CooldownSeconds: 6f,
                ManaCost: 35f,
                Range: 90f,
                AoeRadius: 34f
            ),
        };
}
```

- [ ] **Step 3: Commit**

```powershell
git add Game/Gameplay/Skills/SkillDefinition.cs Game/Gameplay/Skills/SkillDatabase.cs
git commit -m "feat(skills): add skill definitions and default database"
```

---

## Task 4: SkillOrchestrator (pure logic decision engine)

**Files:**
- Create: `Game/Gameplay/Skills/SkillDecision.cs`
- Create: `Game/Gameplay/Skills/SkillOrchestrator.cs`
- Create: `Tests/Gameplay/Skills/SkillOrchestratorTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Tests/Gameplay/Skills/SkillOrchestratorTests.cs`:

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Progression;
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Tests.Gameplay.Skills;

public sealed class SkillOrchestratorTests
{
    [Fact]
    public void Evaluate_WhenCooldownNotReady_ReturnsReject()
    {
        var mana = new ManaModel();
        var cd = new CooldownModel();
        cd.Start(SkillSlot.Skill1, 1f);
        var orch = new SkillOrchestrator();

        var cmd = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: Vector2.Zero,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: false,
            Skill1Pressed: true,
            Skill2Pressed: false,
            Skill3Pressed: false,
            Skill4Pressed: false
        );

        var decision = orch.Evaluate(cmd, SkillDatabase.DefaultBySlot, mana, cd, isBusy: false);
        Assert.Equal(SkillDecisionKind.Reject, decision.Kind);
        Assert.Equal(SkillRejectReason.Cooldown, decision.RejectReason);
    }

    [Fact]
    public void Evaluate_WhenManaNotEnough_ReturnsReject()
    {
        var mana = new ManaModel();
        mana.TryConsume(99f);
        var cd = new CooldownModel();
        var orch = new SkillOrchestrator();

        var cmd = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: Vector2.Zero,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: false,
            Skill1Pressed: true,
            Skill2Pressed: false,
            Skill3Pressed: false,
            Skill4Pressed: false
        );

        var decision = orch.Evaluate(cmd, SkillDatabase.DefaultBySlot, mana, cd, isBusy: false);
        Assert.Equal(SkillDecisionKind.Reject, decision.Kind);
        Assert.Equal(SkillRejectReason.NotEnoughMana, decision.RejectReason);
    }

    [Fact]
    public void Evaluate_WhenSecondaryPressed_ReturnsStartAoeTargeting()
    {
        var mana = new ManaModel();
        var cd = new CooldownModel();
        var orch = new SkillOrchestrator();

        var cmd = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: Vector2.Zero,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: true,
            Skill1Pressed: false,
            Skill2Pressed: false,
            Skill3Pressed: false,
            Skill4Pressed: false
        );

        var decision = orch.Evaluate(cmd, SkillDatabase.DefaultBySlot, mana, cd, isBusy: false);
        Assert.Equal(SkillDecisionKind.StartAoeTargeting, decision.Kind);
        Assert.Equal(SkillSlot.Secondary, decision.Slot);
    }
}
```

- [ ] **Step 2: Implement decision types**

Create `Game/Gameplay/Skills/SkillDecision.cs`:

```csharp
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Skills;

public enum SkillDecisionKind
{
    None = 0,
    StartInstantCast = 1,
    StartAoeTargeting = 2,
    Reject = 3,
}

public enum SkillRejectReason
{
    None = 0,
    Cooldown = 1,
    NotEnoughMana = 2,
    Busy = 3,
}

public readonly record struct SkillDecision(
    SkillDecisionKind Kind,
    SkillSlot Slot,
    SkillRejectReason RejectReason
)
{
    public static SkillDecision None => new(SkillDecisionKind.None, SkillSlot.Primary, SkillRejectReason.None);
    public static SkillDecision StartInstant(SkillSlot slot) =>
        new(SkillDecisionKind.StartInstantCast, slot, SkillRejectReason.None);

    public static SkillDecision StartAoe(SkillSlot slot) =>
        new(SkillDecisionKind.StartAoeTargeting, slot, SkillRejectReason.None);

    public static SkillDecision Reject(SkillRejectReason reason, SkillSlot slot) =>
        new(SkillDecisionKind.Reject, slot, reason);
}
```

- [ ] **Step 3: Implement orchestrator**

Create `Game/Gameplay/Skills/SkillOrchestrator.cs`:

```csharp
using System.Collections.Generic;
using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Progression;

namespace GodotGameTemplate.Gameplay.Skills;

/// <summary>
/// 技能编排器（纯逻辑）：把“按键触发”转换为“进入施法/瞄准状态”的决策。
/// </summary>
public sealed class SkillOrchestrator
{
    public SkillDecision Evaluate(
        PlayerCommand command,
        IReadOnlyDictionary<SkillSlot, SkillDefinition> skills,
        ManaModel mana,
        CooldownModel cooldowns,
        bool isBusy
    )
    {
        if (isBusy)
        {
            if (command.AnySkillPressed)
            {
                return SkillDecision.Reject(SkillRejectReason.Busy, SkillSlot.Primary);
            }

            return SkillDecision.None;
        }

        if (!command.AnySkillPressed)
        {
            return SkillDecision.None;
        }

        // 依序：Secondary 优先（避免同时按下时误触发其它技能）
        var slot = ResolvePressedSlot(command);
        if (!skills.TryGetValue(slot, out var def))
        {
            return SkillDecision.None;
        }

        if (!cooldowns.IsReady(slot))
        {
            return SkillDecision.Reject(SkillRejectReason.Cooldown, slot);
        }

        if (mana.Current < def.ManaCost)
        {
            return SkillDecision.Reject(SkillRejectReason.NotEnoughMana, slot);
        }

        return def.CastType == SkillCastType.AoeTargeting
            ? SkillDecision.StartAoe(slot)
            : SkillDecision.StartInstant(slot);
    }

    private static SkillSlot ResolvePressedSlot(PlayerCommand command)
    {
        if (command.SecondaryPressed)
            return SkillSlot.Secondary;
        if (command.PrimaryPressed)
            return SkillSlot.Primary;
        if (command.Skill1Pressed)
            return SkillSlot.Skill1;
        if (command.Skill2Pressed)
            return SkillSlot.Skill2;
        if (command.Skill3Pressed)
            return SkillSlot.Skill3;
        return SkillSlot.Skill4;
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add Tests/Gameplay/Skills/SkillOrchestratorTests.cs Game/Gameplay/Skills/SkillDecision.cs Game/Gameplay/Skills/SkillOrchestrator.cs
git commit -m "feat(skills): add skill orchestrator decision engine"
```

---

## Task 5: Extend PlayerCommand to support targeting confirm/cancel + aim vector

**Files:**
- Modify: `Game/Gameplay/Input/Commands/PlayerCommand.cs`
- Modify: `Game/Gameplay/Input/MouseKeyboardInputAdapter.cs`
- Modify: `Game/Gameplay/Input/GamepadInputAdapter.cs`
- Modify: `Game/Gameplay/Input/TouchInputAdapter.cs`
- Modify: `Tests/Gameplay/Input/MultiPlatformInputTests.cs` (update record ctor calls)

### Data design
- Add `Vector2 AimVector` (right stick / virtual aim)
- Add `bool ConfirmPressed` / `bool CancelPressed`

- [ ] **Step 1: Update PlayerCommand**

Modify `Game/Gameplay/Input/Commands/PlayerCommand.cs` to:

```csharp
public readonly record struct PlayerCommand(
    Vector2? ClickMoveDestination,
    ulong? ClickTargetInstanceId,
    Vector2 AimVector,
    bool EvadePressed,
    bool InteractPressed,
    bool ToggleInventoryPressed,
    bool ConfirmPressed,
    bool CancelPressed,
    bool PrimaryPressed,
    bool SecondaryPressed,
    bool Skill1Pressed,
    bool Skill2Pressed,
    bool Skill3Pressed,
    bool Skill4Pressed
)
{
    public bool AnySkillPressed =>
        PrimaryPressed || SecondaryPressed || Skill1Pressed || Skill2Pressed || Skill3Pressed || Skill4Pressed;
}
```

- [ ] **Step 2: Update MouseKeyboardInputAdapter**

Modify `Game/Gameplay/Input/MouseKeyboardInputAdapter.cs`:

- `ConfirmPressed`: `Godot.Input.IsMouseButtonPressed(MouseButton.Left)` 的边沿触发（用内部 `_prevLeftDown` 记录）
- `CancelPressed`: `Godot.Input.IsMouseButtonPressed(MouseButton.Right)` 的边沿触发（或 `ui_cancel`/Esc）
- `AimVector`: `Vector2.Zero`（键鼠用鼠标位置，不用向量）

Note: This adapter currently is stateless; you must add private fields for edge detection.

- [ ] **Step 3: Update GamepadInputAdapter**

Modify `Game/Gameplay/Input/GamepadInputAdapter.cs`:

- Add `public Vector2 AimVector { get; private set; }` read from right stick: `JoyAxis.RightX/RightY`
- `ConfirmPressed`: `JustPressed(JoyButton.A, currentButtons)`
- `CancelPressed`: `JustPressed(JoyButton.B, currentButtons)` (NOTE: B currently maps to EvadePressed; keep BOTH true here; targeting state will consume cancel first)
- Keep `EvadePressed` mapping as B as well (so outside targeting it still dodges).

- [ ] **Step 4: Update TouchInputAdapter**

Modify `Game/Gameplay/Input/TouchInputAdapter.cs`:

- Add `Vector2 AimVector { get; private set; }` + `SetVirtualAim(Vector2)`
- Add setters: `SetConfirmPressed(bool)`, `SetCancelPressed(bool)` with edge semantics like other buttons
- In `GetCommand()` fill new fields accordingly.

- [ ] **Step 5: Update tests that construct PlayerCommand**

Update any compilation breaks, especially:
- `Tests/Gameplay/Input/MultiPlatformInputTests.cs`
- Any other tests/fixtures that new-up `PlayerCommand`

- [ ] **Step 6: Run tests + Commit**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj`

Commit:

```powershell
git add Game/Gameplay/Input/Commands/PlayerCommand.cs Game/Gameplay/Input/*.cs Tests/Gameplay/Input/MultiPlatformInputTests.cs
git commit -m "feat(input): add confirm/cancel and aim vector to player command"
```

---

## Task 6: Add skill state fields to ActorContext + GameFeatures switch

**Files:**
- Modify: `Game/Gameplay/Actors/ActorContext.cs`
- Modify: `Game/Config/GameFeatures.cs`

- [ ] **Step 1: Extend GameFeatures**

Modify `Game/Config/GameFeatures.cs`:

```csharp
public bool EnableSkills { get; set; } = true;
```

- [ ] **Step 2: Extend ActorContext**

Modify `Game/Gameplay/Actors/ActorContext.cs` to include:

- `ManaModel Mana { get; } = new();`
- `CooldownModel Cooldowns { get; } = new();`
- `bool IsCasting { get; set; }`
- `bool CastFinishedThisFrame { get; set; }`
- `bool IsTargeting { get; set; }`
- `bool TargetingFinishedThisFrame { get; set; }`

Make sure to reset `CastFinishedThisFrame/TargetingFinishedThisFrame` each `_PhysicsProcess` in `PlayerController` (similar to attack/evade finished flags).

- [ ] **Step 3: Commit**

```powershell
git add Game/Config/GameFeatures.cs Game/Gameplay/Actors/ActorContext.cs
git commit -m "feat(skills): add mana/cooldown and casting flags to actor context"
```

---

## Task 7: Implement AoeIndicator (scene + script)

**Files:**
- Create: `Game/Scenes/Skills/AoeIndicator.tscn`
- Create: `Game/Scenes/Skills/AoeIndicator.cs`

- [ ] **Step 1: Create scene**

Create `Game/Scenes/Skills/AoeIndicator.tscn` (simple Node2D with `Line2D` circle or `Polygon2D`):

```ini
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://Game/Scenes/Skills/AoeIndicator.cs" id="1_indicator"]

[node name="AoeIndicator" type="Node2D"]
script = ExtResource("1_indicator")
```

- [ ] **Step 2: Script**

Create `Game/Scenes/Skills/AoeIndicator.cs`:

```csharp
using Godot;

namespace GodotGameTemplate.Game.Scenes.Skills;

public partial class AoeIndicator : Node2D
{
    [Export]
    public float Radius { get; set; } = 24f;

    public override void _Draw()
    {
        DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 48, new Color(0.2f, 0.9f, 0.9f, 0.8f), 2f);
        DrawCircle(Vector2.Zero, 2f, new Color(0.2f, 0.9f, 0.9f, 0.9f));
    }

    public void SetRadius(float radius)
    {
        Radius = radius;
        QueueRedraw();
    }
}
```

- [ ] **Step 3: Commit**

```powershell
git add Game/Scenes/Skills/AoeIndicator.tscn Game/Scenes/Skills/AoeIndicator.cs
git commit -m "feat(skills): add aoe indicator scene"
```

---

## Task 8: Implement skill effects (Projectile + AOE strike)

**Files:**
- Create: `Game/Scenes/Skills/ProjectileSkillEffect.tscn`
- Create: `Game/Scenes/Skills/ProjectileSkillEffect.cs`
- Create: `Game/Scenes/Skills/AoeStrikeSkillEffect.tscn`
- Create: `Game/Scenes/Skills/AoeStrikeSkillEffect.cs`

- [ ] **Step 1: Projectile effect**

Scene `Game/Scenes/Skills/ProjectileSkillEffect.tscn`:

```ini
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" path="res://Game/Scenes/Skills/ProjectileSkillEffect.cs" id="1_proj"]

[sub_resource type="CircleShape2D" id="CircleShape2D_hit"]
radius = 4.0

[node name="ProjectileSkillEffect" type="Area2D"]
script = ExtResource("1_proj")

[node name="CollisionShape2D" type="CollisionShape2D" parent="."]
shape = SubResource("CircleShape2D_hit")
```

Script `Game/Scenes/Skills/ProjectileSkillEffect.cs`:

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;

namespace GodotGameTemplate.Game.Scenes.Skills;

public partial class ProjectileSkillEffect : Area2D
{
    [Export] public float Speed { get; set; } = 220f;
    [Export] public float LifetimeSeconds { get; set; } = 1.2f;
    [Export] public string AttackId { get; set; } = "skill_projectile";

    public Vector2 Direction { get; set; } = Vector2.Right;

    private double _elapsed;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        _elapsed += delta;
        if (_elapsed >= LifetimeSeconds)
+       {
            QueueFree();
            return;
        }

        GlobalPosition += Direction.Normalized() * Speed * (float)delta;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is IHitReceiver receiver)
        {
            receiver.ReceiveHit(new HitContext(AttackId));
        }

        QueueFree();
    }
}
```

- [ ] **Step 2: AOE strike effect**

Scene `Game/Scenes/Skills/AoeStrikeSkillEffect.tscn`:

```ini
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://Game/Scenes/Skills/AoeStrikeSkillEffect.cs" id="1_aoe"]

[node name="AoeStrikeSkillEffect" type="Node2D"]
script = ExtResource("1_aoe")
```

Script `Game/Scenes/Skills/AoeStrikeSkillEffect.cs`:

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;

namespace GodotGameTemplate.Game.Scenes.Skills;

public partial class AoeStrikeSkillEffect : Node2D
{
    [Export] public float Radius { get; set; } = 20f;
    [Export] public string AttackId { get; set; } = "skill_aoe";
    [Export] public int MaxResults { get; set; } = 16;

    public override void _Ready()
    {
        // 延后到空闲帧执行 IntersectCircle，避免“flushing queries”报错
        CallDeferred(nameof(DoStrikeDeferred));
    }

    private void DoStrikeDeferred()
    {
        var space = GetWorld2D().DirectSpaceState;
        var query = new PhysicsShapeQueryParameters2D
        {
            Transform = new Transform2D(0f, GlobalPosition),
            Shape = new CircleShape2D { Radius = Radius },
            CollideWithAreas = true,
            CollideWithBodies = true,
        };

        var hits = space.IntersectShape(query, MaxResults);
        foreach (var hit in hits)
        {
            var collider = hit["collider"].AsGodotObject();
            if (collider is Node2D n && n is IHitReceiver receiver)
            {
                receiver.ReceiveHit(new HitContext(AttackId));
            }
        }

        QueueFree();
    }
}
```

- [ ] **Step 3: Commit**

```powershell
git add Game/Scenes/Skills/AoeStrikeSkillEffect.* Game/Scenes/Skills/ProjectileSkillEffect.*
git commit -m "feat(skills): add projectile and aoe skill effects"
```

---

## Task 9: Add player states (instant cast + aoe targeting)

**Files:**
- Create: `Game/Gameplay/Player/States/PlayerSkillCastState.cs`
- Create: `Game/Gameplay/Player/States/PlayerAoeTargetingState.cs`
- Modify: `Game/Gameplay/Player/PlayerController.cs`

### Key rules
- Targeting/casting periods must suppress:
  - `ApplyCombatOrchestration()` (no auto chase/attack)
  - Evade and normal attack triggers
  - Left-click world interactions while targeting (left click becomes confirm)

- [ ] **Step 1: Add PlayerSkillCastState**

Create `Game/Gameplay/Player/States/PlayerSkillCastState.cs`:

```csharp
using System;
using Godot;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;
using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Gameplay.Player.States;

public sealed class PlayerSkillCastState : IState<ActorContext>
{
    private readonly Func<SkillSlot, SkillDefinition> _getSkill;
    private readonly Action<SkillDefinition> _spawnInstantEffect;
    private readonly Action<SkillDefinition, Vector2> _spawnAoeEffect;

    private SkillSlot _slot;
    private Vector2 _aoePoint;
    private bool _isAoe;

    private double _elapsed;
    public float MinDurationSeconds { get; set; } = 0.15f;

    public PlayerSkillCastState(
        Func<SkillSlot, SkillDefinition> getSkill,
        Action<SkillDefinition> spawnInstantEffect,
        Action<SkillDefinition, Vector2> spawnAoeEffect
    )
    {
        _getSkill = getSkill;
        _spawnInstantEffect = spawnInstantEffect;
        _spawnAoeEffect = spawnAoeEffect;
    }

    public void ConfigureInstant(SkillSlot slot)
    {
        _slot = slot;
        _isAoe = false;
    }

    public void ConfigureAoe(SkillSlot slot, Vector2 point)
    {
        _slot = slot;
        _aoePoint = point;
        _isAoe = true;
    }

    public void Enter(ActorContext context)
    {
        _elapsed = 0d;
        context.IsCasting = true;
        context.CastFinishedThisFrame = false;
        context.CanMove = false;

        var def = _getSkill(_slot);
        if (!context.Mana.TryConsume(def.ManaCost))
        {
            // 理论上 orchestrator 已阻止；这里防御性处理
            context.IsCasting = false;
            context.CanMove = true;
            context.CastFinishedThisFrame = true;
            return;
        }

        context.Cooldowns.Start(_slot, def.CooldownSeconds);

        if (_isAoe)
            _spawnAoeEffect(def, _aoePoint);
        else
            _spawnInstantEffect(def);
    }

    public void Exit(ActorContext context)
    {
        context.IsCasting = false;
        context.CanMove = true;
    }

    public void Update(ActorContext context, double delta) { }

    public void PhysicsUpdate(ActorContext context, double delta)
    {
        _elapsed += delta;
        if (_elapsed < MinDurationSeconds)
            return;

        context.IsCasting = false;
        context.CanMove = true;
        context.CastFinishedThisFrame = true;
    }
}
```

- [ ] **Step 2: Add PlayerAoeTargetingState**

Create `Game/Gameplay/Player/States/PlayerAoeTargetingState.cs`:

```csharp
using System;
using Godot;
using GodotGameTemplate.Game.Scenes.Skills;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;
using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Gameplay.Player.States;

public sealed class PlayerAoeTargetingState : IState<ActorContext>
{
    private readonly Func<Vector2> _getMouseWorld;
    private readonly Func<Vector2> _getAimVector;
    private readonly Func<bool> _getConfirmPressed;
    private readonly Func<bool> _getCancelPressed;
    private readonly Func<SkillDefinition> _getSecondarySkill;
    private readonly Func<AoeIndicator> _getOrCreateIndicator;

    private AoeIndicator? _indicator;
    private Vector2 _point;

    public Vector2 SelectedPoint => _point;

    public PlayerAoeTargetingState(
        Func<Vector2> getMouseWorld,
        Func<Vector2> getAimVector,
        Func<bool> getConfirmPressed,
        Func<bool> getCancelPressed,
        Func<SkillDefinition> getSecondarySkill,
        Func<AoeIndicator> getOrCreateIndicator
    )
    {
        _getMouseWorld = getMouseWorld;
        _getAimVector = getAimVector;
        _getConfirmPressed = getConfirmPressed;
        _getCancelPressed = getCancelPressed;
        _getSecondarySkill = getSecondarySkill;
        _getOrCreateIndicator = getOrCreateIndicator;
    }

    public void Enter(ActorContext context)
    {
        context.IsTargeting = true;
        context.TargetingFinishedThisFrame = false;
        context.CanMove = false;

        _indicator = _getOrCreateIndicator();
        _indicator.Visible = true;
        _indicator.SetRadius(_getSecondarySkill().AoeRadius);

        _point = _getMouseWorld();
        _indicator.GlobalPosition = _point;
    }

    public void Exit(ActorContext context)
    {
        context.IsTargeting = false;
        context.CanMove = true;
        if (_indicator != null)
            _indicator.Visible = false;
    }

    public void Update(ActorContext context, double delta)
    {
        // 键鼠：跟随鼠标；手柄：右摇杆增量移动
        var aim = _getAimVector();
        if (aim.LengthSquared() > 0.01f)
        {
            _point += aim * 320f * (float)delta;
        }
        else
        {
            _point = _getMouseWorld();
        }

        _indicator?.SetGlobalPosition(_point);

        if (_getCancelPressed())
        {
            context.TargetingFinishedThisFrame = true;
        }
    }

    public void PhysicsUpdate(ActorContext context, double delta)
    {
        if (_getConfirmPressed())
        {
            context.TargetingFinishedThisFrame = true;
        }
    }
}
```

- [ ] **Step 3: Wire states in PlayerController**

Modify `Game/Gameplay/Player/PlayerController.cs`:

- Add fields:
  - `SkillOrchestrator _skillOrchestrator = new();`
  - `PlayerSkillCastState _skillCastState`
  - `PlayerAoeTargetingState _aoeTargetingState`
  - `PackedScene _projectileScene` / `_aoeStrikeScene` / `_indicatorScene`
- In `_Ready()` instantiate states with delegates and load scenes via `GD.Load<PackedScene>("res://...")`.
- In `_PhysicsProcess`:
  - Tick Mana + Cooldowns each frame: `_context.Mana.Tick((float)delta); _context.Cooldowns.Tick((float)delta);`
  - Before `HandleLeftClick()`:
    - If `_context.IsTargeting`: do NOT call `HandleLeftClick` (left click belongs to confirm)
  - Evaluate skills (when `Features.EnableSkills`):
    - Skip if `IsAttacking/IsEvading/IsCasting/IsTargeting`
    - call `SkillOrchestrator.Evaluate(mergedCommand, SkillDatabase.DefaultBySlot, _context.Mana, _context.Cooldowns, isBusy: ...)`
    - If decision is `StartAoeTargeting`: `_stateMachine.ChangeState(_aoeTargetingState)`
    - If decision is `StartInstantCast` and slot != Primary: `_skillCastState.ConfigureInstant(slot); _stateMachine.ChangeState(_skillCastState)`
  - On targeting finish:
    - If confirm was pressed: `_skillCastState.ConfigureAoe(SkillSlot.Secondary, _aoeTargetingState.SelectedPoint); ChangeState(_skillCastState)`
    - Else cancel: ChangeState idle/move
  - Ensure while targeting/casting:
    - do NOT call `ApplyCombatOrchestration()`
    - do NOT allow evade / attack state transitions

- [ ] **Step 4: Commit**

```powershell
git add Game/Gameplay/Player/States/PlayerSkillCastState.cs Game/Gameplay/Player/States/PlayerAoeTargetingState.cs Game/Gameplay/Player/PlayerController.cs
git commit -m "feat(skills): add cast and aoe targeting player states"
```

---

## Task 10: HUD - Mana + skill bar + reject toast

**Files:**
- Modify: `Game/UI/Hud/Hud.tscn`
- Modify: `Game/UI/Hud/Hud.cs`

- [ ] **Step 1: Extend Hud.tscn**

Add nodes:
- `ManaLabel` (Label)
- `SkillBar` (HBoxContainer) with 6 `Label` children or `Button` children.

- [ ] **Step 2: Extend Hud.cs**

Add:
- Display Mana from `_player.ActorContext.Mana`
- For each slot, read cooldown remaining from `_player.ActorContext.Cooldowns.GetRemaining(slot)`
- Gray out if Mana不足（compare `Mana.Current` and `SkillDatabase.DefaultBySlot[slot].ManaCost`）
- Add a 1-second toast label when reject reason occurs (player can expose `LastSkillRejectReason` as a string set by controller).

- [ ] **Step 3: Commit**

```powershell
git add Game/UI/Hud/Hud.tscn Game/UI/Hud/Hud.cs
git commit -m "feat(ui): show mana and skill cooldown bar"
```

---

## Task 11: Session persistence (in-session) + optional save

**Files:**
- Modify: `Game/Gameplay/Session/GameSession.cs`
- (Optional) Modify save DTO/service to include Mana + cooldowns later; skip for first pass.

- [ ] **Step 1: Add Mana/Cooldowns to GameSession**

Add:
- `public ManaModel Mana { get; } = new();`
- `public CooldownModel Cooldowns { get; } = new();`

Modify `PlayerController` to bind `ActorContext.Mana/Cooldowns` to session (either by reference or copy):
- Preferred: `ActorContext` uses session-owned models (inject in controller during _Ready).

- [ ] **Step 2: Commit**

```powershell
git add Game/Gameplay/Session/GameSession.cs Game/Gameplay/Player/PlayerController.cs
git commit -m "feat(skills): persist mana and cooldowns in game session"
```

---

## Task 12: Verification, formatting, UIDs

- [ ] **Step 1: Format**

Run: `csharpier format .`

- [ ] **Step 2: Tests**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj`
Expected: PASS.

- [ ] **Step 3: Godot build**

Run (MCP): `mcp_godot build_project` (via tool)
Expected: 0 errors.

- [ ] **Step 4: Commit generated `.uid` files (if any)**

```powershell
git status --porcelain
git add <new .uid files>
git commit -m "chore: add generated uid metadata for skills"
```

---

## Spec coverage self-check

- Mana + CD: Tasks 1-2
- Skill defs & slots: Task 3
- Pure logic orchestrator + reject: Task 4
- Secondary targeting (mouse/aim) + confirm/cancel: Tasks 5 + 9
- Effects: Task 8
- UI: Task 10
- No regression: Task 9 wiring rules (suppress combat/evade/attack when targeting/casting)
- Tests + build: Task 12

