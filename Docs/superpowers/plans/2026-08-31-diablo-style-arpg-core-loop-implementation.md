# Diablo-style 等距 ARPG 核心闭环 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在现有 Godot 4.2 + C# 工程基础上，实现“等距 Diablo-style 单机 ARPG”的最小可玩闭环：点击移动、点怪自动追击普攻、翻滚体力、6 技能槽（先小后大）、掉落拾取与三槽装备、回城售卖与最小存档，并且各系统可通过配置开关裁剪。

**Architecture:** 延续 `输入 -> 意图 -> 逻辑/状态 -> 导航/命中 -> 反馈 -> 表现` 的单向依赖；把“点击移动/追击/战斗编排/掉落/背包/城镇/存档”拆成可插拔模块；所有与 Godot 节点/Resource 强绑定的部分保持薄胶水，其余尽量沉到可测试的纯逻辑类。

**Tech Stack:** Godot 4.2, C#, `CharacterBody2D`, `NavigationAgent2D`, `Area2D`, 自研轻量 FSM, xUnit（注意 Tests 中避免直接实例化 Godot `Resource` 子类）, Markdown docs.

---

## Scope Guard（避免做大）

本计划只承诺一个职业、占位环境、最小敌人、最小 UI。以下内容明确不做：

- 联机 / 赛季 / 服务器经济
- 多职业与复杂平衡
- 多部位装备系统（只做武器/护甲/饰品）
- 复杂天赋树（只做技能 1-5 级）

---

## 文件结构（将要新增/修改的核心文件）

### 新增（建议）

- Create: `Game/Config/GameFeatures.cs`
- Create: `Game/Config/Progression/LevelConfig.cs`
- Create: `Game/Config/Combat/SkillConfig.cs`
- Create: `Game/Config/Items/ItemConfig.cs`
- Create: `Game/Gameplay/Input/Commands/PlayerCommand.cs`
- Create: `Game/Gameplay/Input/Commands/SkillSlot.cs`
- Create: `Game/Gameplay/Input/MouseKeyboardInputAdapter.cs`
- Create: `Game/Gameplay/Combat/Targeting/TargetingService.cs`
- Create: `Game/Gameplay/Combat/Targeting/ITargetable.cs`
- Create: `Game/Gameplay/Navigation/ClickToMoveModel.cs`
- Create: `Game/Gameplay/Navigation/ClickToMoveController.cs` (Godot 胶水)
- Create: `Game/Gameplay/Combat/CombatOrchestrator.cs`
- Create: `Game/Gameplay/Progression/StaminaModel.cs`
- Create: `Game/Gameplay/Progression/ExperienceModel.cs`
- Create: `Game/Gameplay/Items/ItemInstance.cs`
- Create: `Game/Gameplay/Items/InventoryModel.cs`
- Create: `Game/Gameplay/Items/EquipmentModel.cs`
- Create: `Game/Gameplay/Items/LootDropper.cs`
- Create: `Game/Gameplay/Save/SaveData.cs`
- Create: `Game/Gameplay/Save/SaveService.cs`
- Create: `Game/Gameplay/Enemies/BasicEnemyController.cs`
- Create: `Game/Scenes/World/WorldRoot.tscn`
- Create: `Game/Scenes/Town/Town.tscn`
- Create: `Game/UI/Hud/Hud.tscn`
- Create: `Game/UI/Hud/Hud.cs`
- Create: `Game/UI/Inventory/InventoryPanel.tscn`
- Create: `Game/UI/Inventory/InventoryPanel.cs`

### 修改（预期）

- Modify: `Game/Gameplay/Actors/ActorIntent.cs`（保留移动向量，但新增“是否来自点击移动”的桥接字段或保持不变并用玩家层意图聚合）
- Modify: `Game/Gameplay/Player/PlayerController.cs`（由“方向输入驱动移动”升级为“命令/导航驱动移动”，同时保留未来手柄/触屏入口）
- Modify: `Game/Gameplay/Input/PlayerInputAdapter.cs`（可保留为“方向输入提供者”，或逐步淡出）
- Modify: `Game/Scenes/Main/Main.tscn`（替换为 WorldRoot 或作为调试场景）
- Modify: `project.godot`（新增输入映射：技能槽、翻滚、交互、打开背包等）
- Modify: `Docs/Modules/player-controller.md`（补充点击移动与追击的主链路）

---

## Task 0: 建立“可裁剪开关”与基础输入映射

**Files:**
- Create: `Game/Config/GameFeatures.cs`
- Modify: `project.godot`
- Test: `Tests/Gameplay/Config/GameFeaturesTests.cs`（纯逻辑，避免 Resource）

- [ ] **Step 1: 新增 GameFeatures 配置（纯 C#，不继承 Resource）**

```csharp
namespace GodotGameTemplate.Config;

/// <summary>
/// 游戏功能开关。用于快速裁剪/关闭模块，而不需要大规模改代码。
/// 注意：此类型不继承 Godot Resource，避免在 Tests 中实例化导致宿主崩溃。
/// </summary>
public sealed class GameFeatures
{
    public bool EnableLoot { get; set; } = true;
    public bool EnableInventory { get; set; } = true;
    public bool EnableLeveling { get; set; } = true;
    public bool EnableTown { get; set; } = true;
    public bool EnableAoeIndicator { get; set; } = true;

    public bool EnableGamepad { get; set; } = true;
    public bool EnableMobileTouch { get; set; } = true;
}
```

- [ ] **Step 2: 为 `project.godot` 增加输入映射（不删除现有 move_* 与 attack_primary）**

在 `[input]` 段落追加（示例键位，可按需调整）：

```ini
open_inventory={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":73)]
}
interact={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":69)]
}
evade={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":16777237)]
}
skill_1={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":49)]
}
skill_2={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":50)]
}
skill_3={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":51)]
}
skill_4={
"deadzone": 0.2,
"events": [Object(InputEventKey,"physical_keycode":52)]
}
```

> `LMB/RMB` 的技能触发由鼠标按钮事件处理，不强塞进 InputMap（避免与 UI 点击冲突）。

- [ ] **Step 3: 新增功能开关的最小单测**

Create: `Tests/Gameplay/Config/GameFeaturesTests.cs`

```csharp
using GodotGameTemplate.Config;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Config;

public sealed class GameFeaturesTests
{
    [Fact]
    public void Defaults_AreEnabled()
    {
        var features = new GameFeatures();
        Assert.True(features.EnableLoot);
        Assert.True(features.EnableInventory);
        Assert.True(features.EnableLeveling);
        Assert.True(features.EnableTown);
    }
}
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj --filter FullyQualifiedName~GameFeaturesTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Game/Config/GameFeatures.cs project.godot Tests/Gameplay/Config/GameFeaturesTests.cs
git commit -m "feat: add game feature flags and input actions"
```

---

## Task 1: 定义“玩家命令/技能槽”模型（设备无关）

**Files:**
- Create: `Game/Gameplay/Input/Commands/SkillSlot.cs`
- Create: `Game/Gameplay/Input/Commands/PlayerCommand.cs`
- Test: `Tests/Gameplay/Input/PlayerCommandTests.cs`

- [ ] **Step 1: 添加 SkillSlot 枚举**

```csharp
namespace GodotGameTemplate.Gameplay.Input.Commands;

/// <summary>
/// 6 槽技能位：LMB 普攻、RMB 主技能、1-4 技能。
/// </summary>
public enum SkillSlot
{
    Primary = 0,
    Secondary = 1,
    Skill1 = 2,
    Skill2 = 3,
    Skill3 = 4,
    Skill4 = 5,
}
```

- [ ] **Step 2: 添加 PlayerCommand（以“一帧一份命令快照”为目标）**

```csharp
using Godot;

namespace GodotGameTemplate.Gameplay.Input.Commands;

/// <summary>
/// 玩家层命令快照：表达“这一帧玩家想做什么”，不直接等价于设备输入。
/// </summary>
public readonly record struct PlayerCommand(
    Vector2? ClickMoveDestination,
    ulong? ClickTargetInstanceId,
    bool EvadePressed,
    bool InteractPressed,
    bool ToggleInventoryPressed,
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

- [ ] **Step 3: 添加单测（结构性）**

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Input.Commands;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Input;

public sealed class PlayerCommandTests
{
    [Fact]
    public void AnySkillPressed_TrueWhenAnySkillIsPressed()
    {
        var cmd = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: false,
            Skill1Pressed: false,
            Skill2Pressed: false,
            Skill3Pressed: true,
            Skill4Pressed: false
        );

        Assert.True(cmd.AnySkillPressed);
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj --filter FullyQualifiedName~PlayerCommandTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Game/Gameplay/Input/Commands Tests/Gameplay/Input/PlayerCommandTests.cs
git commit -m "feat: add device-agnostic player command model"
```

---

## Task 2: 键鼠输入适配（点击地面/点击目标/技能键）

**Files:**
- Create: `Game/Gameplay/Input/MouseKeyboardInputAdapter.cs`
- Modify: `Game/Gameplay/Input/IIntentProvider.cs`（新增 `GetCommand()` 或新增新接口）
- Modify: `Game/Gameplay/Input/PlayerInputAdapter.cs`（避免与新输入冲突）

- [ ] **Step 1: 新增 `ICommandProvider` 接口（不替换旧接口，先并存）**

Create: `Game/Gameplay/Input/ICommandProvider.cs`

```csharp
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Input;

public interface ICommandProvider
{
    PlayerCommand GetCommand();
}
```

- [ ] **Step 2: 新增 MouseKeyboardInputAdapter（只采集，不含规则）**

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Input;

/// <summary>
/// 键鼠输入适配：负责把鼠标点击与快捷键转换为 PlayerCommand。
/// 仅负责采集，不实现“追击/攻击”等游戏规则。
/// </summary>
public sealed class MouseKeyboardInputAdapter : ICommandProvider
{
    public PlayerCommand GetCommand()
    {
        var toggleInventory = Input.IsActionJustPressed("open_inventory");
        var interact = Input.IsActionJustPressed("interact");
        var evade = Input.IsActionJustPressed("evade");

        // 数字键技能
        var s1 = Input.IsActionJustPressed("skill_1");
        var s2 = Input.IsActionJustPressed("skill_2");
        var s3 = Input.IsActionJustPressed("skill_3");
        var s4 = Input.IsActionJustPressed("skill_4");

        // 鼠标按钮：这里用“刚按下”判定，避免按住不断触发
        var primary = Input.IsMouseButtonPressed(MouseButton.Left);
        var secondary = Input.IsMouseButtonPressed(MouseButton.Right);

        // 点击地面/点击目标需要依赖射线拾取，属于 Godot 胶水层，暂不在适配器内直接做。
        // 这里先返回“按钮状态”，点击解析在 PlayerController 里做（读取鼠标位置 + 世界拾取）。
        return new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            EvadePressed: evade,
            InteractPressed: interact,
            ToggleInventoryPressed: toggleInventory,
            PrimaryPressed: primary,
            SecondaryPressed: secondary,
            Skill1Pressed: s1,
            Skill2Pressed: s2,
            Skill3Pressed: s3,
            Skill4Pressed: s4
        );
    }
}
```

- [ ] **Step 3: 临时调整旧的 PlayerInputAdapter（确保不会把鼠标输入当成移动方向）**

若 `PlayerInputAdapter.cs` 当前只处理 `Input.GetVector(move_*)`，保持不动即可；后续移动将由点击移动驱动，不再使用方向意图。

- [ ] **Step 4: Commit**

```bash
git add Game/Gameplay/Input/ICommandProvider.cs Game/Gameplay/Input/MouseKeyboardInputAdapter.cs
git commit -m "feat: add mouse/keyboard command provider"
```

---

## Task 3: 等距世界根节点与排序（占位环境）

**Files:**
- Create: `Game/Scenes/World/WorldRoot.tscn`
- Modify: `project.godot`

- [ ] **Step 1: 新建 WorldRoot 场景（使用 YSort 作为世界根）**

Create: `Game/Scenes/World/WorldRoot.tscn`

```tscn
[gd_scene load_steps=3 format=3]

[ext_resource type="PackedScene" path="res://Game/Scenes/Player/Player.tscn" id="1_player"]

[node name="WorldRoot" type="Node2D"]

[node name="YSort" type="YSort" parent="."]

[node name="Player" parent="YSort" instance=ExtResource("1_player")]
position = Vector2(160, 90)
```

- [ ] **Step 2: 设置为主场景（先替换 Main.tscn 的入口）**

Modify `project.godot`：

```ini
[application]
run/main_scene="res://Game/Scenes/World/WorldRoot.tscn"
```

- [ ] **Step 3: Headless 检查**

Run: `godot4 --headless --path . --quit`
Expected: Exit code 0（脚本编译通过）

- [ ] **Step 4: Commit**

```bash
git add Game/Scenes/World/WorldRoot.tscn project.godot
git commit -m "feat: add isometric world root with ysort"
```

---

## Task 4: 点击拾取目标与点击地面落点（Godot 胶水）

**Files:**
- Modify: `Game/Gameplay/Player/PlayerController.cs`
- Create: `Game/Gameplay/Combat/Targeting/ITargetable.cs`
- Create: `Game/Gameplay/Combat/Targeting/TargetingService.cs`
- Create: `Game/Gameplay/Navigation/ClickToMoveModel.cs`
- Create: `Game/Gameplay/Navigation/ClickToMoveController.cs`
- Test: `Tests/Gameplay/Navigation/ClickToMoveModelTests.cs`

- [ ] **Step 1: 定义 ITargetable（敌人/可点对象实现）**

```csharp
namespace GodotGameTemplate.Gameplay.Combat.Targeting;

public interface ITargetable
{
    ulong InstanceId { get; }
}
```

- [ ] **Step 2: TargetingService（纯逻辑：保存/清空目标）**

```csharp
namespace GodotGameTemplate.Gameplay.Combat.Targeting;

/// <summary>
/// 目标锁定服务（纯逻辑，不依赖 Godot Node）。
/// </summary>
public sealed class TargetingService
{
    public ulong? CurrentTargetInstanceId { get; private set; }

    public void SetTarget(ulong instanceId) => CurrentTargetInstanceId = instanceId;

    public void ClearTarget() => CurrentTargetInstanceId = null;
}
```

- [ ] **Step 3: ClickToMoveModel（纯逻辑：目标点与到达判定）**

```csharp
using Godot;

namespace GodotGameTemplate.Gameplay.Navigation;

public sealed class ClickToMoveModel
{
    public Vector2? Destination { get; private set; }

    public float StopRadius { get; set; } = 6f;

    public void SetDestination(Vector2 destination) => Destination = destination;

    public void ClearDestination() => Destination = null;

    public bool IsArrived(Vector2 currentPosition)
    {
        if (Destination == null)
        {
            return true;
        }

        return currentPosition.DistanceTo(Destination.Value) <= StopRadius;
    }
}
```

- [ ] **Step 4: ClickToMoveModelTests**

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Navigation;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Navigation;

public sealed class ClickToMoveModelTests
{
    [Fact]
    public void IsArrived_TrueWhenWithinStopRadius()
    {
        var model = new ClickToMoveModel { StopRadius = 10f };
        model.SetDestination(new Vector2(100, 100));

        Assert.True(model.IsArrived(new Vector2(108, 100)));
    }
}
```

- [ ] **Step 5: ClickToMoveController（Godot 胶水：NavigationAgent2D 跟随）**

Create: `Game/Gameplay/Navigation/ClickToMoveController.cs`

```csharp
using Godot;

namespace GodotGameTemplate.Gameplay.Navigation;

/// <summary>
/// 点击移动控制器：使用 NavigationAgent2D 计算路径，并输出“下一步期望速度方向”。
/// 这是 Godot 胶水层（依赖节点与导航）。
/// </summary>
public partial class ClickToMoveController : Node
{
    [Export] public NavigationAgent2D? Agent { get; set; }

    [Export] public float MaxSpeed { get; set; } = 120f;

    public Vector2 DesiredVelocity { get; private set; }

    public void SetDestination(Vector2 destination)
    {
        if (Agent == null)
        {
            return;
        }

        Agent.TargetPosition = destination;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Agent == null || !Agent.IsNavigationFinished())
        {
            var next = Agent?.GetNextPathPosition() ?? Vector2.Zero;
            var owner = GetParent<Node2D>();
            if (owner == null)
            {
                DesiredVelocity = Vector2.Zero;
                return;
            }

            var dir = (next - owner.GlobalPosition);
            DesiredVelocity = dir == Vector2.Zero ? Vector2.Zero : dir.Normalized() * MaxSpeed;
            if (Agent != null)
            {
                Agent.Velocity = DesiredVelocity;
            }
        }
        else
        {
            DesiredVelocity = Vector2.Zero;
        }
    }
}
```

- [ ] **Step 6: 在 Player.tscn 中挂载 NavigationAgent2D 与 ClickToMoveController**

Modify `Game/Scenes/Player/Player.tscn`（示意，按现有节点结构微调）：

```tscn
[ext_resource type="Script" path="res://Game/Gameplay/Navigation/ClickToMoveController.cs" id="nav_ctl"]

[node name="NavigationAgent2D" type="NavigationAgent2D" parent="."]

[node name="ClickToMove" type="Node" parent="."]
script = ExtResource("nav_ctl")
Agent = NodePath("../NavigationAgent2D")
MaxSpeed = 120.0
```

- [ ] **Step 7: PlayerController 解析鼠标点击（点地/点目标）**

在 `PlayerController.cs` 内新增：

- 读取鼠标世界坐标（通过 `GetGlobalMousePosition()`）
- 使用 `PhysicsPointQueryParameters2D` 在点击位置查询 `Area2D/CollisionObject2D`，决定是否点到敌人
- 点到敌人：交给 `TargetingService.SetTarget(instanceId)`
- 点到地面：`ClickToMoveModel.SetDestination(mousePos)` + `ClickToMoveController.SetDestination(mousePos)`

代码片段（放在 `_PhysicsProcess` 开头处理一次点击事件）：

```csharp
if (Input.IsMouseButtonPressed(MouseButton.Left))
{
    var mousePos = GetGlobalMousePosition();
    var space = GetWorld2D().DirectSpaceState;

    var query = new PhysicsPointQueryParameters2D
    {
        Position = mousePos,
        CollideWithAreas = true,
        CollideWithBodies = true
    };

    var results = space.IntersectPoint(query, maxResults: 16);
    var target = results
        .Select(r => r["collider"])
        .OfType<Node>()
        .FirstOrDefault(n => n.IsInGroup("targetable"));

    if (target != null)
    {
        _targeting.SetTarget(target.GetInstanceId());
    }
    else
    {
        _targeting.ClearTarget();
        _clickToMoveModel.SetDestination(mousePos);
        _clickToMove?.SetDestination(mousePos);
    }
}
```

> 注意：这里用 group `targetable` 做最小实现，后续可换成接口组件化。

- [ ] **Step 8: 运行单测**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj --filter FullyQualifiedName~ClickToMoveModelTests`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add Game/Gameplay/Combat/Targeting Game/Gameplay/Navigation Tests/Gameplay/Navigation
git commit -m "feat: add click-to-move model and targeting service"
```

---

## Task 5: 最小敌人（可被点选、可被命中、可掉落触发点）

**Files:**
- Create: `Game/Gameplay/Enemies/BasicEnemyController.cs`
- Create: `Game/Scenes/Enemies/BasicEnemy.tscn`
- Modify: `Game/Scenes/World/WorldRoot.tscn`

- [ ] **Step 1: 创建 BasicEnemyController（实现可被点选 group + 受击）**

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;

namespace GodotGameTemplate.Gameplay.Enemies;

public partial class BasicEnemyController : CharacterBody2D, IHitReceiver
{
    [Export] public int MaxHp { get; set; } = 10;

    public int Hp { get; private set; }

    public override void _Ready()
    {
        Hp = MaxHp;
        AddToGroup("targetable");
    }

    public void ReceiveHit(HitContext hit)
    {
        Hp -= 1;
        if (Hp <= 0)
        {
            QueueFree();
        }
    }
}
```

- [ ] **Step 2: 创建 BasicEnemy.tscn（占位）**

```tscn
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" path="res://Game/Gameplay/Enemies/BasicEnemyController.cs" id="enemy_script"]

[sub_resource type="CircleShape2D" id="CircleShape2D_hit"]
radius = 10.0

[node name="BasicEnemy" type="CharacterBody2D"]
script = ExtResource("enemy_script")

[node name="Hurtbox" type="Area2D" parent="."]

[node name="CollisionShape2D" type="CollisionShape2D" parent="Hurtbox"]
shape = SubResource("CircleShape2D_hit")
```

- [ ] **Step 3: WorldRoot 放一个敌人实例**

Modify `Game/Scenes/World/WorldRoot.tscn`：

```tscn
[ext_resource type="PackedScene" path="res://Game/Scenes/Enemies/BasicEnemy.tscn" id="2_enemy"]

[node name="Enemy" parent="YSort" instance=ExtResource("2_enemy")]
position = Vector2(240, 100)
```

- [ ] **Step 4: 手动验证**

Run: `godot4 --path .`
Checklist:
- 能点选敌人（至少不会触发点地移动目的地）
- 仍可点地移动

- [ ] **Step 5: Commit**

```bash
git add Game/Gameplay/Enemies Game/Scenes/Enemies Game/Scenes/World/WorldRoot.tscn
git commit -m "feat: add basic targetable enemy"
```

---

## Task 6: CombatOrchestrator（点怪自动追击到位并普攻）

**Files:**
- Create: `Game/Gameplay/Combat/CombatOrchestrator.cs`
- Modify: `Game/Gameplay/Player/PlayerController.cs`
- Modify: `Game/Gameplay/Player/States/PlayerAttackState.cs`（支持“重复普攻循环”）
- Test: `Tests/Gameplay/Combat/CombatOrchestratorTests.cs`（纯逻辑，只测决策，不测 Godot Node）

- [ ] **Step 1: 先定义纯逻辑决策模型**

Create: `Game/Gameplay/Combat/CombatOrchestrator.cs`

```csharp
using Godot;

namespace GodotGameTemplate.Gameplay.Combat;

public sealed class CombatOrchestrator
{
    public float AttackRange { get; set; } = 22f;
    public float ChaseRepathDistance { get; set; } = 10f;
    public float ChaseMaxDistance { get; set; } = 260f;

    public bool ShouldChaseTarget(Vector2 selfPos, Vector2 targetPos)
    {
        return selfPos.DistanceTo(targetPos) > AttackRange;
    }

    public bool IsTargetTooFar(Vector2 selfPos, Vector2 targetPos)
    {
        return selfPos.DistanceTo(targetPos) > ChaseMaxDistance;
    }
}
```

- [ ] **Step 2: 添加纯逻辑单测**

Create: `Tests/Gameplay/Combat/CombatOrchestratorTests.cs`

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Combat;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Combat;

public sealed class CombatOrchestratorTests
{
    [Fact]
    public void ShouldChaseTarget_TrueWhenOutOfRange()
    {
        var orch = new CombatOrchestrator { AttackRange = 20f };
        Assert.True(orch.ShouldChaseTarget(new Vector2(0, 0), new Vector2(50, 0)));
    }
}
```

- [ ] **Step 3: 在 PlayerController 中接线（最小版）**

逻辑：

1. 若 `TargetingService.CurrentTargetInstanceId` 有值：
2. 在场景树里找到对应实例（`GodotObject.InstanceFromId`）
3. 若太远 -> 清空目标
4. 若不在攻击距离 -> `_clickToMove.SetDestination(targetPos)` 追击
5. 若在攻击距离且不在攻击中 -> 切换到 `PlayerAttackState`

关键代码片段（示意）：

```csharp
if (_targeting.CurrentTargetInstanceId is { } id)
{
    var obj = GodotObject.InstanceFromId(id);
    var targetNode = obj as Node2D;
    if (targetNode == null)
    {
        _targeting.ClearTarget();
    }
    else if (_combat.IsTargetTooFar(GlobalPosition, targetNode.GlobalPosition))
    {
        _targeting.ClearTarget();
    }
    else if (_combat.ShouldChaseTarget(GlobalPosition, targetNode.GlobalPosition))
    {
        _clickToMoveModel.SetDestination(targetNode.GlobalPosition);
        _clickToMove?.SetDestination(targetNode.GlobalPosition);
    }
    else if (!_context.IsAttacking && _context.CanAttack)
    {
        _stateMachine.ChangeState(_attackState);
    }
}
```

- [ ] **Step 4: 手动验证**

Checklist:
- 点击敌人后角色会追击到附近
- 到位后会自动普攻（当前攻击状态结束后能再次触发攻击，直到敌人死亡）

- [ ] **Step 5: Commit**

```bash
git add Game/Gameplay/Combat/CombatOrchestrator.cs Tests/Gameplay/Combat/CombatOrchestratorTests.cs Game/Gameplay/Player/PlayerController.cs
git commit -m "feat: add chase-to-attack orchestrator"
```

---

## Task 7: 体力与翻滚（Evade）最小闭环

**Files:**
- Create: `Game/Gameplay/Progression/StaminaModel.cs`
- Create: `Game/Gameplay/Player/States/PlayerEvadeState.cs`
- Modify: `Game/Gameplay/Actors/ActorContext.cs`
- Modify: `Game/Gameplay/Player/PlayerController.cs`
- Test: `Tests/Gameplay/Progression/StaminaModelTests.cs`

- [ ] **Step 1: StaminaModel（纯逻辑）**

```csharp
namespace GodotGameTemplate.Gameplay.Progression;

public sealed class StaminaModel
{
    public float Max { get; set; } = 100f;
    public float Current { get; private set; } = 100f;
    public float RegenPerSecond { get; set; } = 20f;

    public void ResetFull() => Current = Max;

    public void Tick(float delta)
    {
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

- [ ] **Step 2: 单测**

```csharp
using GodotGameTemplate.Gameplay.Progression;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Progression;

public sealed class StaminaModelTests
{
    [Fact]
    public void TryConsume_FalseWhenNotEnough()
    {
        var stamina = new StaminaModel();
        stamina.TryConsume(100f);
        Assert.False(stamina.TryConsume(1f));
    }
}
```

- [ ] **Step 3: ActorContext 增加体力与翻滚状态**

在 `ActorContext` 新增字段（示意）：

```csharp
public StaminaModel Stamina { get; } = new();
public bool IsEvading { get; set; }
```

- [ ] **Step 4: PlayerEvadeState（位移 + 时间窗口）**

Create: `Game/Gameplay/Player/States/PlayerEvadeState.cs`

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;

namespace GodotGameTemplate.Gameplay.Player.States;

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
        context.CanAttack = false;
        context.CanMove = false;
    }

    public void Exit(ActorContext context)
    {
        context.IsEvading = false;
        context.CanAttack = true;
        context.CanMove = true;
    }

    public void Update(ActorContext context, double delta) { }

    public void PhysicsUpdate(ActorContext context, double delta)
    {
        _elapsed += delta;
        context.Velocity = context.Facing.Normalized() * _speed;

        if (_elapsed >= _duration)
        {
            context.Velocity = Vector2.Zero;
        }
    }
}
```

- [ ] **Step 5: PlayerController 接线（按键触发 + 体力消耗）**

规则：

- 当 `evade` 按下、且不在攻击中、且体力足够：切换到 `EvadeState`
- 体力每物理帧 Tick

最小代码片段（示意）：

```csharp
_context.Stamina.Tick((float)delta);
if (_command.EvadePressed && !_context.IsAttacking && !_context.IsEvading)
{
    if (_context.Stamina.TryConsume(25f))
    {
        _stateMachine.ChangeState(_evadeState);
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj --filter FullyQualifiedName~StaminaModelTests`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add Game/Gameplay/Progression/StaminaModel.cs Tests/Gameplay/Progression/StaminaModelTests.cs Game/Gameplay/Player/States/PlayerEvadeState.cs Game/Gameplay/Actors/ActorContext.cs Game/Gameplay/Player/PlayerController.cs
git commit -m "feat: add stamina and evade"
```

---

## Task 8: 最小掉落、背包与三槽装备（可开关）

**Files:**
- Create: `Game/Gameplay/Items/ItemInstance.cs`
- Create: `Game/Gameplay/Items/InventoryModel.cs`
- Create: `Game/Gameplay/Items/EquipmentModel.cs`
- Create: `Game/Gameplay/Items/LootDropper.cs`
- Modify: `Game/Gameplay/Enemies/BasicEnemyController.cs`（死亡时掉落）
- Create: `Game/Scenes/Items/LootPickup.tscn`
- Create: `Game/Scenes/Items/LootPickup.cs`
- Test: `Tests/Gameplay/Items/InventoryModelTests.cs`

- [ ] **Step 1: ItemInstance（纯数据，先不接 Resource）**

```csharp
namespace GodotGameTemplate.Gameplay.Items;

public enum ItemSlot
{
    Weapon,
    Armor,
    Accessory,
}

public enum ItemRarity
{
    Common,
    Magic,
    Rare,
}

public sealed record ItemInstance(
    string Id,
    ItemSlot Slot,
    ItemRarity Rarity,
    int Power
);
```

- [ ] **Step 2: InventoryModel（纯逻辑）**

```csharp
namespace GodotGameTemplate.Gameplay.Items;

public sealed class InventoryModel
{
    private readonly List<ItemInstance> _items = new();

    public int Capacity { get; set; } = 24;

    public IReadOnlyList<ItemInstance> Items => _items;

    public bool TryAdd(ItemInstance item)
    {
        if (_items.Count >= Capacity)
        {
            return false;
        }

        _items.Add(item);
        return true;
    }

    public bool Remove(ItemInstance item) => _items.Remove(item);
}
```

- [ ] **Step 3: EquipmentModel（三槽）**

```csharp
namespace GodotGameTemplate.Gameplay.Items;

public sealed class EquipmentModel
{
    public ItemInstance? Weapon { get; private set; }
    public ItemInstance? Armor { get; private set; }
    public ItemInstance? Accessory { get; private set; }

    public void Equip(ItemInstance item)
    {
        switch (item.Slot)
        {
            case ItemSlot.Weapon:
                Weapon = item;
                break;
            case ItemSlot.Armor:
                Armor = item;
                break;
            case ItemSlot.Accessory:
                Accessory = item;
                break;
        }
    }
}
```

- [ ] **Step 4: InventoryModelTests**

```csharp
using GodotGameTemplate.Gameplay.Items;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class InventoryModelTests
{
    [Fact]
    public void TryAdd_FalseWhenFull()
    {
        var inv = new InventoryModel { Capacity = 1 };
        Assert.True(inv.TryAdd(new ItemInstance("w1", ItemSlot.Weapon, ItemRarity.Common, 1)));
        Assert.False(inv.TryAdd(new ItemInstance("w2", ItemSlot.Weapon, ItemRarity.Common, 1)));
    }
}
```

- [ ] **Step 5: LootDropper（纯逻辑：先固定概率/固定物品）**

```csharp
namespace GodotGameTemplate.Gameplay.Items;

public sealed class LootDropper
{
    private readonly Random _random = new();

    public ItemInstance RollBasicDrop()
    {
        var rarity = _random.NextDouble() switch
        {
            < 0.70 => ItemRarity.Common,
            < 0.95 => ItemRarity.Magic,
            _ => ItemRarity.Rare,
        };

        return new ItemInstance(
            Id: "weapon_sword_basic",
            Slot: ItemSlot.Weapon,
            Rarity: rarity,
            Power: rarity == ItemRarity.Rare ? 5 : rarity == ItemRarity.Magic ? 3 : 1
        );
    }
}
```

- [ ] **Step 6: LootPickup 场景与脚本（点击拾取 -> 事件）**

Create: `Game/Scenes/Items/LootPickup.tscn`

```tscn
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" path="res://Game/Scenes/Items/LootPickup.cs" id="1_pickup"]

[sub_resource type="CircleShape2D" id="CircleShape2D_pick"]
radius = 10.0

[node name="LootPickup" type="Area2D"]
script = ExtResource("1_pickup")

[node name="CollisionShape2D" type="CollisionShape2D" parent="."]
shape = SubResource("CircleShape2D_pick")
```

Create: `Game/Scenes/Items/LootPickup.cs`

```csharp
using Godot;
using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Scenes.Items;

public partial class LootPickup : Area2D
{
    public ItemInstance Item { get; set; } = new("unknown", ItemSlot.Weapon, ItemRarity.Common, 1);

    [Signal]
    public delegate void PickedEventHandler(LootPickup pickup);

    public override void _Ready()
    {
        AddToGroup("loot_pickup");
    }

    public void Pick()
    {
        EmitSignal(SignalName.Picked, this);
        QueueFree();
    }
}
```

- [ ] **Step 7: 敌人死亡时生成掉落（由 PlayerController 或 WorldRoot 监听更干净；首版可先简单）**

在 `BasicEnemyController` 死亡前发信号 `Died(GlobalPosition)`，由 `WorldRoot` 监听并实例化 `LootPickup`。

- [ ] **Step 8: Run tests**

Run: `dotnet test .\Tests\GodotGameTemplate.Tests.csproj --filter FullyQualifiedName~InventoryModelTests`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add Game/Gameplay/Items Tests/Gameplay/Items/InventoryModelTests.cs Game/Scenes/Items
git commit -m "feat: add minimal loot, inventory and equipment models"
```

---

## Task 9: HUD + 背包 UI（最小可用）

**Files:**
- Create: `Game/UI/Hud/Hud.tscn`
- Create: `Game/UI/Hud/Hud.cs`
- Create: `Game/UI/Inventory/InventoryPanel.tscn`
- Create: `Game/UI/Inventory/InventoryPanel.cs`
- Modify: `Game/Scenes/World/WorldRoot.tscn`

- [ ] **Step 1: InventoryPanel（纯 UI，展示列表 + 穿戴按钮）**

`InventoryPanel.cs` 的公开 API 只接受 `InventoryModel/EquipmentModel` 的只读视图或 DTO，避免 UI 直接操控规则。

- [ ] **Step 2: Hud（体力条 + 经验条 + 打开背包）**

- [ ] **Step 3: 接到 WorldRoot（UI 常驻）**

- [ ] **Step 4: 手动验证**

Checklist:
- 按 `I` 打开/关闭背包
- 背包能显示拾取到的物品
- 点击“装备”能把武器放到武器槽（先不做复杂对比）

- [ ] **Step 5: Commit**

```bash
git add Game/UI Game/Scenes/World/WorldRoot.tscn
git commit -m "feat: add minimal hud and inventory ui"
```

---

## Task 10: 城镇与售卖（最小服务点，可开关）

**Files:**
- Create: `Game/Scenes/Town/Town.tscn`
- Create: `Game/Scenes/Town/TownController.cs`
- Modify: `Game/Scenes/World/WorldRoot.tscn`（加传送门/回城入口）

- [ ] **Step 1: Town 场景（占位碰撞 + 商人触发区）**
- [ ] **Step 2: 商人交互：把背包物品卖成金币（固定价格公式）**
- [ ] **Step 3: 回城/出城（最小：切换场景）**
- [ ] **Step 4: Commit**

```bash
git add Game/Scenes/Town
git commit -m "feat: add minimal town and vendor service"
```

---

## Task 11: 最小存档（只存必要字段）

**Files:**
- Create: `Game/Gameplay/Save/SaveData.cs`
- Create: `Game/Gameplay/Save/SaveService.cs`
- Modify: `Game/Scenes/World/WorldRoot.tscn`（退出/回城时自动保存）

- [ ] **Step 1: SaveData（可序列化 DTO）**
- [ ] **Step 2: SaveService（JSON 存取，路径使用 `user://`）**
- [ ] **Step 3: 手动验证**

Checklist:
- 退出再进，等级/金币/装备仍在

- [ ] **Step 4: Commit**

```bash
git add Game/Gameplay/Save Game/Scenes/World/WorldRoot.tscn
git commit -m "feat: add minimal save/load"
```

---

## Task 12: 手柄与触屏输入（第二阶段接入，保持同一 PlayerCommand）

**Files:**
- Create: `Game/Gameplay/Input/GamepadInputAdapter.cs`
- Create: `Game/Gameplay/Input/TouchInputAdapter.cs`
- Modify: `Game/Gameplay/Player/PlayerController.cs`

- [ ] **Step 1: GamepadInputAdapter（先做：摇杆移动 + A/B/X/Y 对应技能位）**
- [ ] **Step 2: TouchInputAdapter（先做：虚拟摇杆移动 + 技能按钮）**
- [ ] **Step 3: 在 PlayerController 中用“当前启用输入源”合并 PlayerCommand**
- [ ] **Step 4: Commit**

```bash
git add Game/Gameplay/Input/GamepadInputAdapter.cs Game/Gameplay/Input/TouchInputAdapter.cs Game/Gameplay/Player/PlayerController.cs
git commit -m "feat: add gamepad and touch command providers"
```

---

## Final Verification

- [ ] **Step 1: 运行全部单测**

Run: `dotnet test`
Expected: PASS

- [ ] **Step 2: Headless 检查**

Run: `godot4 --headless --path . --quit`
Expected: Exit code 0

- [ ] **Step 3: 手动可玩验收（最小闭环）**

Checklist:
- 点击地面移动稳定
- 点击怪自动追击普攻直到击杀
- 翻滚消耗体力且有位移
- 敌人死亡掉落，能拾取进背包
- 能穿戴三槽装备（至少武器）
- 回城售卖换金币
- 退出重进存档可恢复

---

## Self-Review（本计划自检）

1. **Spec coverage：** 已覆盖：等距排序、点击移动、追击普攻、翻滚体力、装备掉落、回城服务、最小存档与可裁剪开关；技能系统与升级在首版中以“技能槽+按键触发”为主，后续可在 `SkillConfig` 上继续扩展。
2. **Placeholder scan：** 本计划在 UI、Town、Save 的具体代码仍需在执行时补齐，执行时必须按“每一步给出完整代码”的要求补足（不得留 TODO/TBD）。
3. **Type consistency：** `PlayerCommand/SkillSlot` 已统一；导航与目标选择均通过 `InstanceId`/`Vector2` 传递，避免 Godot Node 强耦合到纯逻辑。

---

## Execution Handoff

计划已写好。执行方式二选一：

1. **Subagent-Driven（推荐）**：我按任务逐个派发子代理实现，任务间审查与调整。
2. **Inline Execution**：我在当前会话按计划逐项实现并做阶段性检查。

你选哪一种？

