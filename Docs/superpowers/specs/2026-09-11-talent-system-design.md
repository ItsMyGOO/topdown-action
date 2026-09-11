# 技能树/天赋系统设计（含 UI）

## 背景

等级系统已落地（每级 +2 法力/体力、+5 生命），但升级除了数值成长没有「选择」。暗黑式的成长需要**取舍**：天赋点花在哪、走什么Build。装备与伤害/抗性体系已就位，天赋效果有现成的属性面可以作用（伤害/生命/法力/体力/经验/移速）。

本切片实现「升级得 1 点 → 三选一天赋盘 → 分配生效」的最小天赋系统，并按用户要求交付**可操作的天赋面板 UI**（快捷键开关）。

## 目标

- 天赋目录（纯逻辑 `TalentDatabase`）：6 个天赋，每个有名称、描述、最大等级、前置条件：
  | Id | 名称 | 上限 | 前置 | 每级效果 |
  |----|------|------|------|----------|
  | might | 力量 | 3 | 无 | +1 攻击伤害 |
  | toughness | 坚韧 | 3 | might 1 | +10 最大生命 |
  | meditation | 冥想 | 3 | 无 | +5 最大法力 |
  | endurance | 耐力 | 3 | 无 | +10 最大体力 |
  | wisdom | 智慧 | 2 | meditation 1 | +5% 经验获取 |
  | swiftness | 疾行 | 2 | 无 | +8% 移动速度 |
- `TalentModel`（纯逻辑）：各天赋当前等级、已花点数、分配校验（点数足够、不超上限、前置满足）、读档恢复
- 可用点数 = `等级 - 1 - 已花点数`（1 级 0 点，每升 1 级 +1）
- `TalentStats.Aggregate`：把天赋等级聚合成属性加成（伤害/生命/法力/体力/经验%/移速%）
- 生效路径：
  - 伤害：`AttackDamage = 武器 Power + 力量加成`
  - 生命：`LevelingModel` 新增 `ExternalHealthBonus`，与等级成长绝对式合并
  - 法力/体力：并入既有 `External*Bonus`
  - 经验：总倍率 = 装备倍率 × 天赋倍率（`WorldRoot`）
  - 移速：`ClickToMove.MaxSpeed = 基础速度 × 天赋倍率`
- **UI**：`TalentPanel`（动态行：名称、等级 x/y、效果说明、[+] 按钮）；HUD 按 `open_talents`（T 键）切换；面板头部显示可用点数；不满足条件时按钮禁用
- 存档：天赋等级持久化（`SaveData.Talents`），旧档缺省为空（未加点）

## 非目标

- 洗点/重置（后续可加按钮）
- 主动技能解锁与强化（天赋只做被动属性）
- 多天赋页/Build 导入导出
- 天赋树连线可视化（首版列表式）

## 方案比选

- **方案 A（采用）**：静态目录 + 字典状态 + 聚合器，UI 动态生成行。与 InventoryPanel 同构，纯逻辑全量可测。
- 方案 B：Godot `Resource` 定义天赋树。违反「规则纯逻辑化」约束，编辑器外不可测。
- 方案 C：通用修饰符管线。等 Buff/Debuff 出现再说。

## 模块设计

### 纯逻辑（`Game/Gameplay/Progression/Talents/`）

- `TalentDefinition(string Id, string Name, string Description, int MaxRank, string? RequiresId = null, int RequiresRank = 0)`
- `TalentDatabase.Catalog`（IReadOnlyList）+ `Get(id)`；目录完整性（id 唯一、前置引用存在）有测试兜底
- `TalentModel`：
  - `IReadOnlyDictionary<string, int> Ranks`、`PointsSpent`
  - `int AvailablePoints(int level) => level - 1 - PointsSpent`（钳 0）
  - `bool CanAllocate(string id, int level)`：存在、未满级、点数可用、前置达标
  - `bool Allocate(string id, int level)`：校验通过则 +1 级并返回 true
  - `void Restore(IEnumerable<(string id, int rank)>)`：读档恢复（非法条目忽略）
- `TalentStats.Aggregate(TalentModel)` → `TalentStatSummary(int BonusDamage, float BonusMaxHealth, float BonusMaxMana, float BonusMaxStamina, float XpMultiplier, float MoveSpeedMultiplier)`

### LevelingModel 扩展

- `ExternalHealthBonus`（默认 0）；`DesiredHealthMax` 与 `ApplyGrowth` 并入（绝对式，读档幂等不变）

### 存档

- `SaveTalentData(string Id, int Rank)` DTO + `SaveData.Talents`（默认空列表）；mapper 往返；旧档反序列化为空

### Godot 胶水

- `GameSession.Talents`（持有模型，参与存档）
- `PlayerController`：
  - `TalentStats` 聚合缓存每帧在 `ReconcileLevelGrowth` 中刷新（装备+天赋合并写入 `External*Bonus`/`ExternalHealthBonus`）
  - `AttackDamage` 计入 `BonusDamage`；`_PhysicsProcess` 中 `ClickToMove.MaxSpeed = Config.MoveSpeed * talents.MoveSpeedMultiplier`
- `WorldRoot.OnEnemyDied`：经验倍率 = 装备 × 天赋
- `TalentPanel`（`Game/UI/Talents/`）：`Refresh()` 重建行；行内 `[+]` 调 `TalentModel.Allocate` 后刷新；HUD `_Ready` 缓存引用、`_Process` 监听 `open_talents`
- `project.godot`：新增 `open_talents` 输入映射（物理键 T）

## 测试计划

- `TalentDatabaseTests`：目录非空、id 唯一、前置引用存在且要求等级 ≤ 上限
- `TalentModelTests`：初始为空；分配/点数耗尽/满级/前置不满足/未知天赋；`AvailablePoints` 钳 0；Restore（含非法条目）
- `TalentStatsTests`：空模型全零；多天赋聚合；经验/移速为乘区
- `LevelingModelTests`（扩展）：`ExternalHealthBonus` 并入期望值与 ApplyGrowth、读档幂等
- `GameSessionSaveTests`（扩展）：天赋往返、旧档缺字段
- 引擎冒烟：升到 3 级（2 点）→ 分配 力量×2 → `AttackDamage` 增加 2；分配 冥想×1 → 法力上限 +5；天赋持久化后 Load 不丢

## 可裁剪性

- 天赋 UI 受新输入映射控制，关闭映射即不可打开；模型无点数时自然不可分配，无需独立功能开关
