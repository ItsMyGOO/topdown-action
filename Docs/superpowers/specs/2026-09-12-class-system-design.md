# 第四期：职业系统（野蛮人/法师/游侠）与职业技能组

## 背景

当前是「无职业」单角色：全职业共享同一套技能表（`SkillDatabase.DefaultBySlot` 静态硬编码）与同一组成长倍率。D4 的核心身份系统是职业：不同职业有独特技能组与属性倾向。装备/天赋/伤害体系已就位，职业是最能放大它们玩法的下一块拼图。

## 目标

- **职业目录**（纯逻辑 `ClassDatabase`）：3 个 D4 风格职业雏形
  | Id | 名称 | 倾向 | 伤害 | 生命 | 法力 | 移速 |
  |----|------|------|------|------|------|------|
  | barbarian | 野蛮人 | 近战厚血高伤 | ×1.3 | ×1.25 | ×0.6 | ×1.0 |
  | sorcerer | 法师 | 远程法术脆皮 | ×0.9 | ×0.8 | ×1.5 | ×1.0 |
  | rogue | 游侠 | 敏捷均衡机动 | ×1.1 | ×1.0 | ×0.9 | ×1.2 |
- **职业技能组**：`ClassDatabase.SkillsBySlot(classId)` 返回该职业的 6 槽技能定义——
  - 共享骨架：Primary 普攻 / Secondary 选点 AOE / 1-4 主动，参数（伤害面=Power 无、CD、耗蓝、射程、AOE 半径）按职业侧重调整（野蛮人短程高伤低蓝、法师长程高蓝、游侠机动低 CD）
  - 技能 Id 带职业前缀（`barb_secondary_whirlwind` 等），效果种类（Projectile/AoeStrike）首版共享
- **会话与存档**：`GameSession.ClassId`（默认 `barbarian`，选后存档）；旧档兼容默认
- **属性结算**：`PlayerController.ReconcileLevelGrowth` 并入职业倍率（攻击伤害、生命/法力期望值、ClickToMove 移速）
- **切换入口**：城镇商人区新增「职业石像」Area2D，按 E 交互循环切换职业（单机调试语义；未来主菜单创建角色后移除），切换后立即重结算并刷新 HUD 技能栏
- HUD 显示当前职业名

## 非目标

- 各职业专属技能效果种类（旋风斩转圈、陷阱、传送等表现层差异）
- 角色创建主菜单流程（石像切换是过渡方案）
- 职业专属成长曲线/被动

## 方案比选

- **方案 A（采用）**：静态职业目录 + 按职业的技能表查询函数 + 会话持有职业 Id + 结算时并入倍率。与天赋/词缀同一套「纯逻辑聚合」模式。
- 方案 B：职业做成 Godot Resource。违背纯逻辑可测约束。
- 方案 C：职业差异做成词条/天赋的组合。语义混乱，职业应是第一公民。

## 模块设计

### 纯逻辑（`Game/Gameplay/Progression/Classes/`）

- `ClassDefinition(string Id, string Name, string Description, float DamageMultiplier, float HealthMultiplier, float ManaMultiplier, float MoveSpeedMultiplier)`
- `ClassDatabase.Catalog`（3 条）+ `Get(id)`（未知回退 barbarian）+ `DefaultClassId = "barbarian"`
- `SkillDatabase.SkillsFor(string classId)`：按职业前缀生成 6 槽定义（参数矩阵集中在该函数内，便于调平衡）

### 胶水

- `GameSession.ClassId`（属性 + 存档 `SaveData.ClassId`，旧档默认）
- `PlayerController`：
  - `ClassDefinition Class => ClassDatabase.Get(_session.ClassId)`
  - `AttackDamage` 乘 `Class.DamageMultiplier`（取整）
  - `ReconcileLevelGrowth`：生命/法力期望值乘职业倍率（`External*Bonus` 之外再乘，绝对式幂等保持）
  - 技能查询全部改走 `SkillDatabase.SkillsFor(_session.ClassId)`
  - 移速乘 `Class.MoveSpeedMultiplier`
- `Town.tscn` 加 `ClassShrine`（Area2D + 交互），`TownController` 处理切换：循环三职业 → 存档 → HUD 刷新
- HUD：`ClassLabel` 显示职业名

## 测试计划

- `ClassDatabaseTests`：目录 3 条、id 唯一、Get 未知回退默认、倍率在合理区间
- `SkillDatabaseTests`（扩展）：每职业 6 槽齐全、Id 前缀正确、法系技能 ManaCost > 战士、全职业 Primary 无耗蓝
- `GameSessionSaveTests`（扩展）：ClassId 往返、旧档默认 barbarian
- 引擎冒烟：切职业后攻击伤害/生命上限按倍率变化；技能表随职业变化；存档往返保留职业

## 可裁剪性

- 职业只影响查询函数与倍率；不选（默认蛮人）即回到单职业行为
