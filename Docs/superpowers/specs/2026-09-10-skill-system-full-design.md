## 目标

把现有 6 槽输入（`Primary/Secondary/Skill1-4`）补成一个可玩的 ARPG 技能系统，满足：

- 6 槽都能真正触发并产生效果（至少 2-3 种效果类型）
- 具备冷却（CD）与资源消耗（Mana）
- Secondary 支持地面选点指示器（鼠标/右摇杆移动准星），并且跨平台确认/取消一致
- 键鼠/手柄/触屏统一通过 `PlayerCommand` 驱动
- UI 能展示技能栏（CD/资源不足）、Mana 与基础提示
- 不破坏现有：点击移动、追击自动普攻、翻滚体力、拾取/背包/城镇/存档
- 具备最小单测覆盖纯逻辑部分

## 既有系统盘点（与本设计的对接点）

- 输入：`Game/Gameplay/Input/Commands/PlayerCommand.cs` 提供 6 槽按下布尔；键鼠/手柄/触屏适配器已经能产出 `PlayerCommand`
- 行为状态：`PlayerAttackState`（普攻）、`PlayerEvadeState`（翻滚），由 `PlayerController` 的 `_stateMachine` 驱动
- 战斗：命中接口 `IHitReceiver` + `HitContext`，普攻使用 `PlayerAttackHitbox`
- 目标：`TargetingService` 保存当前 `targetable` 目标实例 id；`CombatOrchestrator` 负责追击到位自动普攻
- 资源：已有 `StaminaModel` 用于翻滚；本设计新增 `ManaModel` 用于技能
- UI：已有 `Hud` 与 `InventoryPanel`，可扩展出 Mana 与技能栏
- 会话与存档：`GameSession` 为 Autoload，保存 `Inventory/Equipment/Gold`；存档系统已完成，后续需要把 Mana/技能冷却纳入（本轮先做会话内持久，存档可选）

## 关键交互约定（已确认）

### 技能类型约定

- `Primary`：沿用现有普攻（`PlayerAttackState`），作为“技能槽 0”。可以设置 `ManaCost = 0`，但仍可走公共冷却接口（默认不加 CD）
- `Secondary`：地面选点技能（`AoeTargeting`）
- `Skill1-4`：瞬发技能（`Instant`）

### Secondary 地面选点

- 进入瞄准：按 `Secondary`
- 瞄准点移动：
  - 键鼠：跟随鼠标位置（世界坐标）
  - 手柄：右摇杆移动准星（世界坐标偏移）；左摇杆仍可驱动角色移动（但建议瞄准期间限制移动，见后文）
- 确认/取消（跨平台一致）：
  - 键鼠：左键确认、右键取消
  - 手柄：A 确认、B 取消
- 瞄准期间：
  - 不触发追击/自动普攻
  - 不允许翻滚与其它技能（避免状态冲突）

## 架构方案（推荐：状态机驱动施法）

将“是否能放、放什么、怎么放”拆成纯逻辑与 Godot 胶水两层：

### 纯逻辑层（可单测）

#### 1) ManaModel（新增）

接口与 `StaminaModel` 对齐：

- 字段：`Max`、`Current`、`RegenPerSecond`
- 方法：`Tick(delta)`、`TryConsume(amount)`

#### 2) CooldownModel（新增）

管理每个 `SkillSlot` 的冷却：

- `float GetRemaining(SkillSlot slot)`
- `bool IsReady(SkillSlot slot)`
- `void Start(SkillSlot slot, float seconds)`
- `void Tick(float delta)`

#### 3) SkillDefinition + SkillDatabase（新增）

`SkillDefinition` 描述一个技能的公共数据：

- `SkillId`（string/enum，首版可用 string）
- `SkillSlot Slot`
- `CastType`：`Instant` / `AoeTargeting`
- `CooldownSeconds`
- `ManaCost`
- `Range`
- `AoeRadius`
- `EffectKind`：`MeleeSlash` / `Projectile` / `AoeStrike`（首版至少实现 Projectile 与 AoeStrike）

`SkillDatabase`：一份静态/可注入的技能表，首版可以硬编码 6 个技能定义（后续再改成资源/表格）。

#### 4) SkillOrchestrator（新增）

纯逻辑判定“这一帧的技能按钮触发，是否能进入施法/瞄准，或直接施放”：

输入：

- `PlayerCommand`（只看技能槽按下）
- `CooldownModel`、`ManaModel`
- 目标信息快照：是否有合法目标、距离
- 当前角色状态：是否在攻击/翻滚/瞄准中

输出：

- `SkillDecision`：
  - `StartInstantCast(SkillSlot slot)`
  - `StartAoeTargeting(SkillSlot slot)`
  - `None`
  - `Reject(reason)`（用于 UI 提示：冷却中、Mana 不足、没有目标等）

注意：具体世界坐标、命中检测、投射物生成不属于纯逻辑层。

### Godot 胶水层（状态机 + 效果节点）

#### 1) PlayerSkillCastState（新增）

- Enter：
  - 由 `SkillDefinition` 扣 Mana（`TryConsume`），并启动 CD（`Cooldown.Start`）
  - 根据 `EffectKind` 生成对应效果节点（见后文）
  - 设置 `context.CanMove = false`（短暂后摇），并设置 `IsCasting = true`
- PhysicsUpdate：
  - 满足最小时长后退出，`CastFinishedThisFrame = true`
- Exit：
  - 恢复 `CanMove`

#### 2) PlayerAoeTargetingState（新增）

- Enter：
  - `IsTargeting = true`，`CanMove = false`
  - 创建/显示 `AoeIndicator`（可复用现有功能开关 `EnableAoeIndicator`）
  - 初始化落点为“鼠标位置（键鼠）”或“面向前方一定距离（手柄）”
- Update/PhysicsUpdate：
  - 键鼠：落点跟随鼠标世界坐标
  - 手柄：右摇杆增量移动准星；支持 deadzone
  - 处理确认/取消：
    - 确认：转入 `PlayerSkillCastState`，并把落点作为参数
    - 取消：退出，不消耗 Mana，不进入 CD
- Exit：
  - 隐藏/释放指示器，恢复 `CanMove`

#### 3) 技能效果节点（最小实现）

至少实现：

- `ProjectileSkillEffect`：直线飞行，命中 `IHitReceiver` 时调用 `ReceiveHit`，然后销毁
- `AoeStrikeSkillEffect`：在确认落点创建一次性范围检测（`DirectSpaceState.IntersectCircle` 或 `Area2D`），对范围内目标调用 `ReceiveHit`

规范：

- 效果节点必须在创建时传入 `AttackId` 或 `SkillId` 作为 `HitContext` 的来源标识
- 避免在物理查询 flushing 阶段修改监测状态（必要时 `CallDeferred`）

## 目标选择与落点规则

统一规则（Instant 类技能）：

1. 如果有当前 `targetable` 且在 `Range` 内：以目标为施放点/方向参考
2. 否则：
   - 键鼠：以鼠标方向/鼠标位置为参考（可限制最大距离）
   - 手柄/触屏：以角色面向方向为参考

Secondary（AoeTargeting）：

- 始终以指示器落点为施放点（在最大距离内 clamp）

## 输入合并与消费

原则：仍以 `PlayerCommand` 作为唯一入口；`PlayerController` 负责把多个输入源合并为一个 `PlayerCommand`。

新增的输入需求：

- 瞄准确认/取消：
  - 键鼠：左键确认、右键取消（新增读取鼠标键状态的接口）
  - 手柄：A 确认、B 取消（若在瞄准状态，A/B 优先消费，不再作为普攻/翻滚）

触屏：

- `TouchInputAdapter` 后续可通过 UI 调用 `SetSkillPressed` 与 `SetClickMoveDestination`；
- Secondary 的瞄准：首版允许触屏直接“点地即落点并确认”，或用占位 UI 按钮确认/取消（实现阶段再定）。

## UI 设计（最小可用）

### HUD 扩展

在现有 `Hud` 下新增：

- Mana 文本（`法力: current/max`）或简易 ProgressBar
- 技能栏（6 槽）：
  - 显示每槽的 `CooldownRemaining`（数字）
  - 当 Mana 不足时变灰
  - 当处于瞄准状态时高亮 Secondary

### Debug 提示（可选）

当 `SkillDecision.Reject` 时，在 HUD 显示 1 秒提示（例如 “法力不足”、“冷却中”）。

## 测试策略

单测覆盖纯逻辑：

- `ManaModelTests`：Tick 回复、TryConsume 成功/失败
- `CooldownModelTests`：Start/IsReady/Tick 行为
- `SkillOrchestratorTests`：
  - 冷却中拒绝
  - Mana 不足拒绝
  - Secondary 进入 AoeTargeting
  - Skill1 进入 InstantCast

Godot 侧只做 build 验证（MCP build_project）与最小手动测试，不强求自动化 UI 测试。

## 里程碑与验收（完成定义）

技能系统“补完整”验收条件：

1. 6 槽均可触发：
   - Primary：普攻仍可用
   - Secondary：进入瞄准 -> 确认后产生 AOE 伤害
   - Skill1-4：至少 2 个能造成伤害（Projectile/AoeStrike）
2. Mana 消耗与回复生效，Mana 不足时无法释放并提示
3. 每个技能都有 CD，CD 未结束无法释放并提示
4. 瞄准期间跨平台确认/取消生效，且不触发追击/普攻/翻滚
5. HUD 可显示 Mana 与技能 CD
6. `dotnet test` 全通过，`mcp_godot build_project` 通过

