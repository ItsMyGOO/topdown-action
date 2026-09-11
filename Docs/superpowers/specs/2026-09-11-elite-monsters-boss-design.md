# 精英词缀怪与 Boss 设计

## 背景

当前敌人是均质化的静态木桩：`MaxHp = 3`、固定 5 经验、掉落一件普通物品。没有精英词缀与 Boss，「打怪变强 → 挑战更强怪 → 更好掉落」的暗黑难度阶梯缺失。等级与装备词条系统已落地，需要它们可感知的消费场景。

本切片引入精英词缀怪（随机词缀、强化属性、提升掉落）与 Boss（世界内固定强化怪），让刷怪循环出现「普通 → 精英 → Boss」的目标层次。

## 目标

- 纯逻辑 `ElitePlan`：一次战斗强化计划（词缀、倍率、金币奖励、掉落数量、掉落稀有度下限），由数值在计划生成时全部定死，胶水层只执行
- 三种精英词缀（首版只做当前系统可感知的效果）：
  - `Sturdy`（强壮）：生命 ×3、经验 ×2
  - `Cunning`（狡诈）：经验 ×2、掉落 2 件且稀有度不低于魔法
  - `Rich`（富有）：击杀立即掉落金币 10~25
- `EliteRoller`：默认 15% 概率把普通怪升为精英（可注入种子做确定性测试）；Boss 为固定强化计划（生命 ×10、经验 ×10、掉落 2 件稀有、金币 50）
- `LootDropper.RollDrop(rarityFloor)`：稀有度抽取带下限（精英/Boss 掉落规则复用现有生成器）
- `BasicEnemyController.ApplyPlan(plan)`：应用倍率到 `MaxHp`/`XpReward`，词缀染色与 Boss 放大（表现层）
- `WorldRoot` 生成时掷精英、死亡时发放金币奖励并按计划生成掉落
- World 场景放置一个 Boss
- 存档兼容：精英/Boss 为运行时状态，不持久化（读档后世界重置为普通怪）

## 非目标

- 敌人移动/追击 AI（速度类词缀等 AI 落地后再加）
- 怪物血条 UI、词缀名称飘字
- 传奇级词缀、词缀组合（首版每精英一个词缀）
- 敌人等级与玩家等级缩放
- Boss 专属技能/阶段机制

## 方案比选

- **方案 A（采用）**：`ElitePlan` 把数值在生成期定死 + 敌人节点持有计划 + `WorldRoot` 逐敌订阅死亡事件。纯逻辑可全量单测，胶水最小。
- 方案 B：词缀做成 Godot `Resource` 挂敌人节点。违背「规则纯逻辑化」约束，不可单测。
- 方案 C：通用敌人属性缩放系统（按玩家等级动态缩放）。范围过大，留待敌人深度下一迭代。

## 模块设计

### ElitePlan / EliteAffix（纯逻辑，`Game/Gameplay/Enemies/`）

- `EliteAffix` 枚举：`None, Sturdy, Cunning, Rich`
- `ElitePlan(bool IsBoss, EliteAffix Affix, float HpMultiplier, float XpMultiplier, int GoldBonus, int DropCount, ItemRarity RarityFloor)` 只读 record

### EliteRoller（纯逻辑）

- `TryRoll(double chance, Random random)`：未命中返回 `null`；命中时按词缀池等概率抽一个词缀并生成计划（`Rich` 的金币数在此期掷定）
- `Boss()`：固定 Boss 计划
- 常量 `EliteChance = 0.15`

### LootDropper 扩展（纯逻辑）

- `RollDrop(ItemRarity rarityFloor)`：现有 `RollBasicDrop` 逻辑 + 稀有度下限钳制（词缀预算跟随最终稀有度）；`RollBasicDrop()` 保留为 `rarityFloor = Common` 的特例

### BasicEnemyController（Godot 胶水）

- `[Export] bool IsBoss`；新增只读 `ElitePlan? Plan`
- `ApplyPlan(ElitePlan plan)`：`MaxHp/Hp`、`XpReward` 按倍率调整，`Body` 按词缀染色，Boss 时放大 `Body` 视觉

### WorldRoot（Godot 胶水）

- `_Ready` 逐敌：`IsBoss` 用 `EliteRoller.Boss()`，否则 15% 掷精英；命中则 `ApplyPlan`；死亡事件改为逐敌 lambda 订阅以携带该敌计划
- 死亡处理：金币奖励（`Rich`/Boss）直接入会话；掉落按 `DropCount`/`RarityFloor` 延后生成

### World 场景

- 追加 `Boss` 节点（`BasicEnemy.tscn` 实例，`IsBoss = true`，置于怪群深处）

## 测试计划

- `EliteRollerTests`：概率 0/1 边界；词缀与计划数值映射；`Rich` 金币区间；Boss 计划；同种子确定性
- `LootDropperTests`（扩展）：`RollDrop` 稀有度下限钳制（含 Rare 下限时词缀数必为 2）
- 胶水以构建 + 引擎冒烟兜底：Boss 击杀 → 经验 ×10、金币入账、掉落稀有

## 可裁剪性

- 精英化仅发生在 `WorldRoot` 生成期，不触碰存档与其它系统；未来加开关（如 `EnableElites`）只需在掷点处短路
