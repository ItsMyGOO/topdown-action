# 多部位装备与词缀设计

## 背景

`2026-08-31 核心闭环` 的非目标清单里明确延后了「暗黑式多部位装备（头/胸/手/腿/鞋/戒指）」。当前实现是 3 槽（武器/护甲/饰品）+ `Power` 数值，且 `Power` 没有任何实际效果。等级系统落地后，成长来源单一（只有等级），缺少暗黑循环「掉落驱动变强」的核心支柱。

本切片把装备扩成暗黑式 8 槽 + 随机词条，让「击杀 → 掉落 → 穿装 → 变强」真正成立，并让 `Power` 保持现状（伤害体系留待敌人深度切片）。

## 目标

- 槽位 3 → 8：`Weapon` / `Armor`(胸甲) / `Accessory`(护符) / `Helmet` / `Gloves` / `Legs` / `Boots` / `Ring`；枚举只追加成员不改旧值，旧存档兼容
- `ItemInstance` 支持词缀行（属性 + 数值），按稀有度给词条数量预算：`Common 0` / `Magic 1` / `Rare 2`
- 首版词条池：`BonusMaxMana`(+5~15)、`BonusMaxStamina`(+5~15)、`BonusXpPercent`(+5%~20%)
- 掉落生成覆盖全部槽位；`LootDropper` 可注入随机种子，词条生成可确定性测试
- 统一属性结算 `EquipmentStats`：装备加成与等级加成合并为最终上限，互不覆盖
- `BonusXpPercent` 实际影响 `WorldRoot` 的经验发放
- 存档兼容：旧档无词条/新槽位字段 → 空词条/未穿戴
- 背包 UI 行内展示词条文本（如「+5 最大法力」）

## 非目标

- 词条重随/工艺、传奇与暗金品质、套装效果、双戒指槽
- 武器伤害体系重做（`Power` 语义维持现状，留给敌人与战斗深度切片）
- 出售价格与词条挂钩、词缀过滤 UI

## 方案比选

- **方案 A（采用）**：扩展现有枚举与模型（字典槽位）+ 统一结算点（`LevelingModel` 增加 `External*Bonus`，玩家侧单点合并结算）。
- 方案 B：`ItemInstance` 改为 Godot `Resource` + `SubResource` 词条。违背「物品模型纯逻辑、不依赖 Godot」的既有约束，单测困难。
- 方案 C：引入通用修饰符聚合系统（modifier/aggregate）。过度设计，等属性面板与更多属性来源出现后再抽象。

## 模块设计

### ItemSlot / AffixStat / AffixLine（纯逻辑，Items 目录）

- `ItemSlot` 追加 `Helmet, Gloves, Legs, Boots, Ring`（值接续旧成员，不改动旧值）
- `AffixStat` 枚举：`BonusMaxMana, BonusMaxStamina, BonusXpPercent`
- `AffixLine(AffixStat Stat, float Value)` 只读 record

### ItemInstance

- 由 record 改为 `sealed class`（本项目没有对它使用 `with` 表达式），保留位置主构造与 ToString
- 新增末位可选参数 `AffixLine[]? Affixes = null`（规范化为不可变副本，永不为 null）
- 手写相等：逐字段 + 词条序列（顺序敏感）相等，保证存档往返相等断言成立

### EquipmentModel

- 槽位存储改为 `Dictionary<ItemSlot, ItemInstance?>`；`Equip(item)` 校验 `item.Slot` 后覆盖该槽
- 新增 `Get(ItemSlot)`、`IEnumerable<(ItemSlot, ItemInstance?)>` 遍历；保留 `Weapon/Armor/Accessory` 兼容属性与 `Clear`
- `SetEquipment` 扩展为全槽位参数（命名参数可选），保留旧三参调用的编译兼容（用可选参数实现）

### EquipmentStats（纯逻辑聚合）

- `Summarize(EquipmentModel)` → `(MaxManaBonus, MaxStaminaBonus, XpMultiplier)`；`XpMultiplier = 1 + sum(BonusXpPercent)/100`

### LevelingModel（与等级加成合并结算）

- 新增可写属性 `ExternalManaBonus` / `ExternalStaminaBonus`（默认 0）
- `ApplyGrowth` 保持绝对式幂等：`Max = Base + 每级加成×(Level-1) + External*Bonus`

### LootDropper / AffixTable（纯逻辑生成）

- `LootDropper` 构造可注入 `Random`（默认无种子）
- `AffixTable.Roll(rarity, random)`：按稀有度词条数量预算，从词条池抽属性与数值区间
- `RollBasicDrop`：槽位 8 选 1、稀有度概率不变（70/25/5）、`Power` 规则不变、追加词条

### 存档

- `SaveItemInstanceData` 新增 `List<SaveAffixLineData>? Affixes`（旧档反序列化为 null → 空）
- `SaveEquipmentSlotsData` 追加 `Helmet/Gloves/Legs/Boots/Ring` 可空字段
- Mapper 用全槽位遍历读写；词条往返与旧档缺字段用单测覆盖

### 胶水

- `PlayerController.ReconcileLevelGrowth`：结算前先从 `EquipmentStats.Summarize` 写入 `Leveling.External*Bonus`，再 `ApplyGrowth`，保证「基础 + 等级 + 装备」一次算清
- `WorldRoot.OnEnemyDied`：经验按 `(int)(XpReward * XpMultiplier)` 发放
- `InventoryPanel`：物品行追加词条文本；装备行读取兼容属性改为全槽位展示

## 测试计划

- `ItemInstanceTests`：相等（含词条顺序敏感）、Affixes 非空规范化
- `EquipmentModelTests`：Equip 校验、全槽位 Get/Set、Clear、旧三参 SetEquipment 兼容
- `EquipmentStatsTests`：空装备、多词条聚合、XpMultiplier 边界
- `LevelingModelTests`（扩展）：External 加成与等级加成合并、读档重结算幂等
- `LootDropperTests`：固定种子确定性、词条数量符合稀有度预算、数值落在区间
- `GameSessionSaveTests`（扩展）：词条往返、新槽位往返、旧档缺字段
- Godot 胶水以构建 + 按需引擎冒烟兜底

## 可裁剪性

- 不新增开关：装备系统已受 `EnableLoot`/`EnableInventory` 管控；词条是物品数据的一部分，随物品一起被管控
