# 等级/经验成长系统设计

## 背景

`2026-08-31 Diablo-style ARPG 核心闭环` 设计目标中包含「角色等级/经验」，`GameFeatures.EnableLeveling` 开关已存在且默认开启，但代码库中没有任何等级/经验实现：击杀敌人只掉落物品，角色没有成长，「杀怪 → 升级 → 变强」的暗黑式循环缺失核心一环。

本切片补齐该缺口，沿用已定型的架构模式（纯逻辑模型 + 会话持久化 + Godot 胶水 + HUD 轮询），与 `ManaModel`/`CooldownModel` 的接入方式保持一致。

## 目标

- 击杀敌人获得经验；经验满自动升级，支持一次跨越多级
- 升级成长：每级 `+2` 最大法力、`+2` 最大体力（数值集中在模型常量中，可调）
- 存档接入：`SaveData` 增加 `Level`/`CurrentXp`，旧存档兼容（缺省 `1`/`0`）
- HUD 显示等级与经验进度；`EnableLeveling = false` 时隐藏
- `EnableLeveling = false` 时不产生任何经验事件
- 纯逻辑部分（模型、存档映射）具备单测

## 非目标

- 主属性系统（力量/敏捷/智力）与属性面板
- 技能点、技能树、天赋盘
- 敌人等级、难度缩放、经验随怪物等级变化
- 玩家生命值模型与死亡（玩家当前无 HP 概念）
- 升级特效演出（先做文本提示，演出留待表现层迭代）

## 方案比选

- **方案 A（采用）：纯逻辑 `LevelingModel` + `GameSession` 持有 + `WorldRoot` 发放经验 + HUD 轮询。** 与 `Mana`/`Cooldowns` 的接入模式完全一致，纯逻辑可单测，胶水最少。
- 方案 B：信号总线事件驱动（敌人 → XP 总线 → 玩家）。解耦更彻底但增加 Godot 胶水与订阅生命周期管理，与现有轮询风格不符。
- 方案 C：现在就引入完整角色属性表。过度设计（YAGNI），属性表应留给装备词缀/天赋切片一起设计。

## 模块设计

### LevelingModel（纯逻辑，`Game/Gameplay/Progression/`）

不依赖 Godot API，与 `ManaModel`/`StaminaModel` 风格一致：

- `Level`：当前等级，从 `1` 开始，单调递增
- `CurrentXp`：当前等级内已积累经验，升级时结转溢出部分
- `XpToNextLevel => BaseXpPerLevel * Level`：升级所需经验（线性曲线，`BaseXpPerLevel = 50`；后续可替换曲线而不改调用方）
- `AddXp(int amount)`：负数与零忽略；返回 `LevelUpResult(int LevelsGained, int NewLevel)`（只读 record）
- 升级加成常量：`BonusManaPerLevel = 2`、`BonusStaminaPerLevel = 2`（每级，相对 1 级基准）
- `ApplyGrowth(StaminaModel? stamina, ManaModel? mana)`：把尚未应用的等级加成增量应用到传入模型（内部以 `AppliedGrowthLevel` 记账，幂等；允许传 `null` 跳过其一）
- `Restore(int level, int xp)`：读档入口；把成长记账重置为 1 级，等待外部重新 `ApplyGrowth` 补发加成（资源模型的 Max 不随存档持久化）

### 经验来源（Godot 胶水）

- `BasicEnemyController` 增加 `[Export] int XpReward = 5`，`Died` 信号签名扩展为 `(Vector2 pos, int xpReward)`
- `WorldRoot.OnEnemyDied`：在 `Features.EnableLeveling` 且会话存在时调用 `_session.Leveling.AddXp(xpReward)`；只发放经验，不做加成结算

### 玩家成长结算（Godot 胶水）

- `PlayerController` 在帧逻辑中检测 `Leveling.AppliedGrowthLevel != Leveling.Level`（且 `EnableLeveling`），对 `_context.Stamina` 与 `_context.Mana` 统一调用 `ApplyGrowth`，并在升级时弹出提示；读档后的加成补发也由此覆盖，模型内部记账保证不会重复加成
- 玩家暴露只读 `Leveling`（功能关闭时为 `null`）供 HUD 判断显隐与读取进度

### 存档

- `SaveData` 增加 `int Level = 1`、`int CurrentXp = 0`；`SaveDataMapper.FromState/ApplyToState` 增加对应参数
- `GameSession` 增加 `LevelingModel Leveling { get; }`，`ToSaveData`/`ApplySaveData` 接入；旧档 JSON 缺字段时反序列化得到默认值 `1`/`0`

### HUD

- 新增 `LevelLabel`：显示 `等级: {Level}  经验: {Current}/{XpToNext}`；`EnableLeveling = false` 或无玩家时隐藏/显示占位，与现有标签行为一致

## 测试计划

- `LevelingModelTests`：初始值；`AddXp` 累计；恰好升级、跨多级溢出结转；负数/零忽略；曲线随等级单调不减；`ApplyGrowth` 增量正确、重复调用幂等、`null` 参数安全
- `SaveDataMapper` 扩展用例：等级/经验往返一致；旧格式（缺字段）应用后为 1/0
- Godot 胶水层不写自动化测试（遵循现有惯例），以构建 + 人工验证兜底

## 可裁剪性

- `EnableLeveling = false`：`WorldRoot` 不发经验、HUD 隐藏标签；存档字段仍读写但不影响玩法，关闭与开启不丢数据
