# 敌人移动 AI 与追击设计

## 背景

所有敌人都是静止木桩：接触伤害只有玩家主动贴脸才会发生，没有威胁、没有走位、没有「引怪」。伤害/抗性体系落地后，战斗需要空间博弈才成立。同时精英词缀池缺少速度类词缀（`2026-09-11 精英与 Boss` 设计中明确留待 AI 落地）。

本切片给普通敌人加上「警戒 → 追击 → 脱战回家」的最小 AI，并新增迅捷词缀。

## 目标

- 纯逻辑 `EnemyAiOrchestrator`：无状态决策器，输入快照（自身/出生点/玩家位置、警戒/脱战/追击上限距离、玩家存活），输出状态（`Idle/Chase/Return`）、移动方向与「到家回满血」标记
- 决策规则（无状态，含迟滞）：
  - 玩家死亡 → `Idle`
  - 距出生点超过 `LeashRange` → `Return`（防风筝）
  - 玩家距离 ≤ `AggroRange` → `Chase`；≤ `DeaggroRange`（= 1.25×Aggro，迟滞防抖）且未超 leash 时继续追
  - `Return` 途中距出生点 ≤ 4px 视为到家：回满血、转 `Idle`
- `BasicEnemyController`：新增 `Speed`（60）、`AggroRange`（140）、`LeashRange`（260）导出属性；`_PhysicsProcess` 按决策 `MoveAndSlide`；到家回满血；接触伤害逻辑保持不变
- `ElitePlan` 新增 `SpeedMultiplier`（默认 1）；新词缀 `Swift`（迅捷：速度 ×1.6、经验 ×1.25）加入精英词缀池；Boss 速度 ×1.15
- 存档兼容：AI 为运行时行为，无持久化字段

## 非目标

- 攻击前摇/后摇、技能型敌人、远程敌人
- 寻路（NavigationServer）：开阔场地直线追击即可
- 敌人间阵型、拉怪仇恨表、召唤与逃跑行为
- 敌人血条与受击表现升级

## 方案比选

- **方案 A（采用）**：无状态决策器 + 每帧快照。与 `CombatOrchestrator` 同构，完全可单测，敌人节点只执行。
- 方案 B：有状态机（保留上一帧状态）。迟滞靠状态记忆更「正统」，但状态藏进节点不可单测；无状态迟滞（脱战半径 > 警戒半径）已满足需求。
- 方案 C：NavigationAgent2D 寻路。当前地图无障碍物，纯开销。

## 模块设计

### 纯逻辑（`Game/Gameplay/Enemies/`）

- `EnemyAiState` 枚举：`Idle, Chase, Return`
- `EnemyAiSnapshot(Vector2 Position, Vector2 HomePosition, Vector2 PlayerPosition, float AggroRange, float DeaggroRange, float LeashRange, bool PlayerAlive)`
- `EnemyAiDecision(EnemyAiState State, Vector2 Direction, bool ReachedHome)`：`Direction` 为归一化移动方向（`Idle` 时为零向量；`Chase` 指向玩家、`Return` 指向出生点）
- `EnemyAiOrchestrator.Evaluate(snapshot)`：
  1. 玩家死亡 → `Idle`
  2. `homeDist > LeashRange` → `Return`（方向指向家）
  3. `homeDist ≤ 4px`（已在出生点附近且玩家不在警戒内）→ `Idle` + `ReachedHome = true`（仅当玩家超出 Deaggro）
  4. `playerDist ≤ AggroRange`，或（`playerDist ≤ DeaggroRange` 且 `homeDist ≤ LeashRange`）→ `Chase`
  5. 其余 → `Idle`

### Godot 胶水（`BasicEnemyController`）

- 导出属性：`Speed = 60`、`AggroRange = 140`、`LeashRange = 260`
- `_Ready` 记录 `HomePosition = GlobalPosition`
- `_PhysicsProcess`：先做既有触碰伤害，再评估 AI → `Velocity = Direction * Speed * planSpeed` → `MoveAndSlide`；`ReachedHome` 时 `Hp = MaxHp`
- `ApplyPlan`：`Speed` 乘 `plan.SpeedMultiplier`

### ElitePlan 扩展

- 追加 `float SpeedMultiplier = 1f`（带默认值，旧构造兼容）
- 词缀池加入 `Swift`：速度 ×1.6、经验 ×1.25、其余不变；Boss `SpeedMultiplier = 1.15`

## 测试计划

- `EnemyAiOrchestratorTests`：
  - 玩家远且在警戒外 → `Idle`
  - 玩家进入警戒圈 → `Chase`，方向指向玩家
  - 迟滞：玩家距离在 Aggro 与 Deaggro 之间 → 继续追（未超 leash 时）
  - 超出 leash → `Return` 指向家
  - 回到出生点附近且玩家脱离 → `Idle` + `ReachedHome`
  - 玩家死亡 → `Idle`
  - 各状态 `Direction` 归一化/零向量正确
- `EliteRollerTests`（扩展）：`Swift` 数值映射；Boss 速度倍率
- 引擎冒烟：敌人位于玩家警戒圈内 → 数帧后敌我距离缩短（真实移动）；接触伤害仍生效

## 可裁剪性

- 不新增开关；AI 完全体现在敌人节点自身，`Speed = 0` 即退化回静止木桩
