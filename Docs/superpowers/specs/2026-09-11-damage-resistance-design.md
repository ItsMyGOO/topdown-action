# 伤害/抗性体系设计

## 背景

当前战斗是「贴脸点名」：`BasicEnemyController.ReceiveHit` 固定扣 1 血，`HitContext` 不携带伤害；`ItemInstance.Power` 只是个没有任何效果的展示数值；玩家没有生命、没有死亡，敌人打不了人。等级与装备词条已落地，但缺乏「装备 → 输出/生存 → 挑战更强怪」的转换层。

本切片让 `Power` 真正生效（武器=伤害、防具=护甲），引入玩家生命与死亡复活、敌人接触伤害与抗性，建立最小伤害/抗性数学。

## 目标

- `HitContext` 携带 `Damage`（默认 1，兼容既有调用）
- `DamageMath.Apply(raw, resistFlat)`：纯逻辑减法抗性公式 `max(1, raw - resist)`（保底 1 点）
- 玩家输出：`武器 Power = 攻击伤害`（未持武器为 1）；`PlayerAttackHitbox` 新增 `Damage` 属性，攻击时由 `PlayerController` 注入
- 玩家生存：`HealthModel`（纯逻辑，形状对齐 `ManaModel`：Max/Current/Tick 回血/TakeDamage/Heal/ResetFull/IsEmpty）；基础 100、每级 +5（并入 `LevelingModel.ApplyGrowth` 的绝对式结算）
- 玩家护甲：已装备防具（非武器槽）的 `Power` 之和 = 护甲，减免接触伤害（并入 `EquipmentStats.Summarize`）
- 敌人输出：`BasicEnemyController` 新增 `Damage`（默认 8）与触碰伤害（贴近玩家每 0.8s 一次）；`ResistFlat`（默认 0）减免玩家伤害
- 精英/Boss：`ElitePlan` 新增 `DamageMultiplier`（强壮 ×2、Boss ×3，其余 ×1），`ApplyPlan` 一并应用
- 玩家死亡：生命归零 → 延后切回城镇场景并满血复活（无惩罚，首版不与存档交互）
- HUD 增加「生命: xx/xx」
- 存档兼容：本切片无新增持久化字段（生命为运行时状态，复活即满血）

## 非目标

- 暴击、攻速、命中率、元素伤害与分系抗性
- 药水/血球、死亡惩罚（装备掉落/经验损失）
- 敌人移动 AI（触碰伤害先以静止敌人贴近为前提）
- 敌人血条 UI
- 生命上限的装备词缀（+HP 词缀留待词缀池扩展）

## 方案比选

- **方案 A（采用）**：`HitContext` 携带伤害 + `DamageMath` 纯公式 + 双向减法抗性（玩家护甲 / 敌人抗性）+ `LevelingModel` 并入生命成长。
- 方案 B：属性修饰符聚合管线（多来源 buff/debuff 栈）。等天赋/技能树出现后再抽象，现在过度设计。
- 方案 C：敌人侧也走 `HitContext` 广播（伤害事件总线）。首版触碰伤害直接调用 `PlayerController.TakeDamage`，链路最短可测。

## 模块设计

### 纯逻辑

- `DamageMath`（Combat）：`Apply(int raw, int resistFlat)`，保底 1
- `HealthModel`（Progression）：`Max=100`、`RegenPerSecond=3`、`TakeDamage`（钳 0）、`Heal`（钳 Max）、`Tick` 回血、`IsEmpty`、`ResetFull`、`ClampToMax`
- `LevelingModel`：`BaseHealthMax=100`、`BonusHealthPerLevel=5`、`DesiredHealthMax`；`ApplyGrowth` 增加可选 `HealthModel?` 参数（绝对式赋值后 `ClampToMax`）
- `ElitePlan` 追加 `float DamageMultiplier`（带默认值 1f，旧调用兼容）；`EliteRoller`：强壮 ×2、Boss ×3、其余 ×1
- `EquipmentStats.Summarize` 追加 `int Armor`（= 非武器已装备槽 `Power` 之和）
- `HitContext` 追加 `int Damage = 1`

### Godot 胶水

- `PlayerAttackHitbox`：`[Export] int Damage = 1`，`TryHit` 构造 `HitContext` 时携带
- `PlayerController`：`AttackDamage`（武器 Power）与 `Armor`（装备聚合）只读属性；每帧同步 `AttackHitbox.Damage`；`ActorContext.Health` 回血 Tick；`ReconcileLevelGrowth` 传入 Health；`TakeDamage(raw)`：护甲减免 → 扣血 → 归零则标记并 `CallDeferred` 回城镇复活（满血、无惩罚）
- `BasicEnemyController`：`[Export] Damage=8`、`[Export] ResistFlat=0`；`ReceiveHit` 走 `DamageMath`；`_PhysicsProcess` 贴近玩家 26px 且冷却 0.8s 时调用 `player.TakeDamage(Damage)`；`ApplyPlan` 应用 `DamageMultiplier`
- `Hud`：`LifeLabel`（始终显示，无玩家时占位）

## 测试计划

- `HealthModelTests`：初始满血；扣血钳 0；`IsEmpty`；回血钳上限；`Tick`；`ClampToMax`；`ResetFull`
- `DamageMathTests`：减抗、保底 1、零/负抗性
- `EquipmentStatsTests`（扩展）：武器不计入护甲、防具 Power 求和
- `EliteRollerTests`（扩展）：强壮/Boss 伤害倍率、其余为 1
- `LevelingModelTests`（扩展）：生命成长并入绝对式结算、读档幂等
- 引擎冒烟：穿甲玩家受触碰伤害（减免生效）、武器 Power 决定击杀速度；死亡复活路径以单测覆盖（冒烟避免切场景）

## 可裁剪性

- 不新增开关：触碰伤害随敌人存在，`EnableSkills`/`EnableLeveling` 等既有开关不受影响
