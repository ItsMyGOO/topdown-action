# 第二期：生命药水、死亡惩罚与生命球掉落（战斗资源闭环）

## 背景

玩家已有生命/死亡/回城复活，但：没有主动回复手段（只能站桩等 3/秒自回）、死亡零惩罚、击杀掉落只有装备没有消耗品。战斗资源循环「缺血 → 用药 → 缺药 → 回城补给」缺失。

## 目标

- **生命药水（D4 式 4 充能）**：
  - `PotionChargesModel`（纯逻辑）：默认 4 充能，`TryConsume`/`Refill`/`Restore`（读档）
  - 使用：新输入映射 `use_potion`（物理键 Q）；每次恢复最大生命 35%（仅生命未满且有余充时可用）
  - 回城自动补满（进入城镇场景时 `TownController` 调 `Refill`；死亡回城复活同样经过城镇 → 一致）
  - 存档：`SaveData.Potions` 记录当前充能数，旧档缺省 4
- **死亡惩罚（简化版：经验 + 金币损失，装备耐久留待后续）**：
  - `LevelingModel.LoseProgress(fraction)`：损失当前等级内经验的 10%（钳 0，**不掉级**）
  - 金币损失 10%（向下取整）
  - 死亡时由 `PlayerController.ApplyDeathPenalty` 应用（`EnableLeveling` 管控）；常量集中在 `LevelingModel.DeathXpLossFraction`/`DeathGoldLossFraction`
- **生命球掉落**：
  - 普通怪死亡 30% 概率掉落生命球；精英必掉；Boss 掉 2 颗
  - 生命球触碰自动拾取，恢复玩家最大生命 10%（D4 血球范式）
  - 表现：像素红球（生成器新增 `orb.png` 12×12），放大镜样式复用拾取物层
- HUD 新增「药水: x/4」标签

## 非目标

- 装备耐久与修理（后续与商人 UI 一起做）
- 药水掉落物（药水只来自回城补给）、生命球对敌人生效
- 暴击/攻速等战斗数值扩展

## 方案比选

- **方案 A（采用）**：`PotionChargesModel` 充能池 + `LevelingModel.LoseProgress` + 生命球复用 Area2D 触碰模式。全部纯逻辑可测，胶水沿用既有模式。
- 方案 B：药水做背包物品（占格子/拾取）。D4 药水是独立充能系统不入包，方案 A 才是对的。
- 方案 C：生命球走掉落物点击拾取。D4 血球是走过自动吸取，点击违背范式。

## 模块设计

### 纯逻辑

- `PotionChargesModel`：`MaxCharges`（构造默认 4）、`Available`、`TryConsume()`、`Refill()`、`Restore(int)`（钳 0..Max）
- `LevelingModel`：`const double DeathXpLossFraction = 0.1`、`DeathGoldLossFraction = 0.1`（金币比例给会话用）；`LoseProgress(double fraction)` 只减 `CurrentXp` 钳 0
- `GameSession.DeathGoldLossFraction` 复用 LevelingModel 常量

### Godot 胶水

- `GameSession.Potions`（模型持有 + 存档读写）
- `PlayerController`：`_Process`/输入侧消费 `use_potion` → 扣充能 + `Health.Heal(0.35 * Max)`；死亡分支应用惩罚（经验/金币）后再排队回城
- `TownController._Ready`：`session.Potions.Refill()`
- `WorldRoot.OnEnemyDied`：`CallDeferred` 掷 30%（精英必掉、Boss 2 颗）生成 `HealthOrb`
- `HealthOrb`（`Game/Scenes/Items/`）：`Area2D`，`body_entered` 玩家 → `Health.Heal(10% Max)` + 自毁
- HUD「药水」标签；`project.godot` 增加 `use_potion` 输入；生成器追加 `orb.png`

## 测试计划

- `PotionChargesModelTests`：初始 4；消耗递减；空拒绝；Refill 满仓；Restore 钳制；自定义上限
- `LevelingModelTests`（扩展）：`LoseProgress` 正常损失、钳 0、不掉级、0 比例不变
- `GameSessionSaveTests`（扩展）：药水充能往返、旧档缺省 4
- 引擎冒烟：用药回血且扣充能；死亡后经验/金币被扣且未掉级；精英击杀必掉生命球、拾取回血

## 可裁剪性

- 三件套互相独立（药水/惩罚/血球），各自集中在单文件；关闭 `EnableLeveling` 时死亡惩罚完全跳过
