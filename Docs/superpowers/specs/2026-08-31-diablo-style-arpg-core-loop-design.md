# Diablo-style 等距 ARPG 最小闭环设计（路线 A）

## 背景

当前工程已具备俯视角 2D 动作基础设施：`输入 -> 意图 -> 逻辑/状态 -> 命中判定 -> 反馈 -> 表现` 的解耦链路、可复用轻量状态机、玩家移动与基础攻击、最小测试体系与文档落位。

本设计的目标是在不推翻现有工程规范与分层的前提下，把项目推进到“像 Diablo 的单机 ARPG”最小可玩闭环，并且所有新增系统都应可裁剪、可替换、可用配置开关关闭，以便后续迭代中按需丢弃某些模块。

角色美术使用外部角色生成素材包（8 方向 top-down / isometric 角色动画）。本设计不会复刻任何 Diablo 的剧情、角色、UI 细节或具体数值表，仅参考其战斗节奏、操作范式与循环结构。

## 目标

本切片完成后，项目应具备以下能力：

- 等距（Isometric）视觉呈现与遮挡排序（基于 2D 坐标与 YSort/自定义排序）
- PC 端鼠标点击移动：点地面移动、点怪物自动追击到攻击距离并自动普攻
- 统一技能槽模型：`6` 槽（`LMB` 普攻、`RMB` 技能、`1-4` 技能），并可映射到手柄与移动端触控
- 统一瞄准模型：以“智能指向”为主，少量技能支持地面指示器落点（按住预览、松开释放）
- 单一职业（近战）最小技能组合：普攻 + 闪避/翻滚 + 若干可替换技能（数量先小后大）
- 体力资源（精力/体力）：自动回复，技能/翻滚消耗
- 轻量装备：`武器 + 护甲 + 饰品` 三槽，支持稀有度与少量有效词条
- 掉落与拾取：击杀掉落、地面拾取、背包、穿戴与简单售卖（回城服务）
- 最小流程闭环：城镇 -> 出门 -> 战斗掉落 -> 回城整理/售卖 -> 再出门
- 最小存档：角色等级/经验、三槽装备、背包、关键流程进度
- “可裁剪”要求落地：每个系统具备明确边界与开关，不需要大范围改代码即可关闭

## 非目标

本切片明确不包含以下内容（可作为后续扩展）：

- 联机/多人、赛季、排行榜、服务器经济系统
- 完整 Diablo 级别的任务线、剧情、过场与语音演出
- 大量职业与完整职业平衡
- 完整暗黑式多部位装备系统（头/胸/手/腿/鞋/戒指等）
- 复杂天赋盘/Paragon 等长线成长系统
- 复杂 AI（导航编队、阵型、技能脚本编辑器等）
- 高度拟真的伤害计算与抗性系统（首版只需“能打死、有掉落、有反馈”）

## 总体方案（路线 A）

### 分层不变

继续沿用并强化当前工程分层：

`输入 -> 意图 -> 逻辑/状态 -> 导航/命中 -> 反馈 -> 表现`

关键约束：

- 输入层只负责读取设备（键鼠/手柄/触屏），不包含游戏规则
- 意图层表达“玩家此刻想做什么”，并保持设备无关
- 逻辑层决定“能不能做、怎么做”（追击、进入攻击状态、消耗体力、冷却等）
- 表现层只同步结果（朝向、动画、特效、UI），不拥有规则

### 可裁剪开关

新增统一的功能开关配置（例如 `GameFeatures`），集中控制模块是否启用。典型开关包括：

- `EnableLoot`：掉落与拾取
- `EnableInventory`：背包与装备
- `EnableLeveling`：等级与经验
- `EnableTown`：城镇服务（商人/传送门/治疗）
- `EnableAOEIndicator`：地面指示器类技能
- `EnableGamepad` / `EnableMobileTouch`：不同输入源（默认都启用）

关闭模块时要求：

- 不再产生对应逻辑事件
- UI 自动隐藏对应入口
- 存档不读写对应字段（或在加载时忽略缺失）

## 模块设计

### 输入与意图

#### 输入源（可插拔）

- `MouseKeyboardInputAdapter`：读取鼠标点击、快捷键（`LMB/RMB/1-4`）与 UI 交互
- `GamepadInputAdapter`：读取摇杆移动/指向、技能按钮、确认/取消
- `TouchInputAdapter`：读取虚拟摇杆/拖拽指向、技能按钮、点击交互

它们都产出同一套意图数据（建议将意图扩展为“结构化命令”而非仅方向向量）：

- `MoveCommand`：目的地坐标（点地移动）或方向（手柄/触摸摇杆）
- `TargetCommand`：选中目标（点怪、辅助锁定）
- `SkillCommand`：槽位索引（0..5） + 目标信息（目标/方向/落点）
- `InteractCommand`：交互（拾取、开箱、对话等）

> 说明：为保持现有 `ActorIntent` 轻量，建议用“聚合意图”对象承载这些命令，例如 `PlayerIntent`（仅玩家层），并在进入 Actor 逻辑层时拆分成 `ActorIntent + CombatIntent` 等更稳定的子意图。

### 目标选择（Targeting）

新增 `TargetingService`：

- 支持从点击对象确定目标
- 支持“最近敌人/朝向优先”的辅助锁定（用于手柄/触屏）
- 支持目标失效（死亡、离开范围）后自动清空或切换

输出：当前锁定目标引用（建议只暴露 `NodePath` / `InstanceId`，避免强耦合）。

### 点击移动与追击（ClickToMove / Chase）

新增 `ClickToMoveController`：

- 点地：设置目的地并请求导航路径（基于 Godot 2D Navigation）
- 追击：当存在攻击目标但距离不足，自动将目的地更新为“目标附近的可达点”
- 到达判定：使用 `StopRadius`（到达半径）与速度阈值判断“已到达”
- 失败处理：路径不可达/路径断裂时中止并反馈（UI 提示或角色停下）

> 注：导航查询属于引擎层；路径跟随与“何时重算”属于逻辑层，可拆成可测试的纯逻辑策略。

### 战斗编排（Combat Orchestrator）

新增 `CombatOrchestrator`，负责把“选目标 + 移动 + 攻击/技能”串成暗黑式体验：

- 点怪：设置目标并进入“追击-攻击”编排
- 当 `InAttackRange` 且技能可用：进入攻击/技能状态
- 当目标移动：追击保持；当超出追踪范围：退出并回到待机
- 支持技能施放的三种目标模式：
  - `Targeted`：有目标就打目标
  - `Directional`：朝方向释放
  - `Ground`：地面落点（受 `EnableAOEIndicator` 开关控制）

### 状态机与角色上下文

沿用现有 `StateMachine<ActorContext>`，扩展 `ActorContext` / 玩家上下文以覆盖：

- `Facing`：逻辑朝向
- `MoveMode`：点地移动/追击移动/手柄方向移动（逻辑统一入口）
- `IsAttacking`、`CanMove`、`CanAttack`
- `Stamina`：当前体力、回复速率、消耗
- `CurrentTarget`：目标引用（可为空）
- `Cooldowns`：技能冷却（首版可用简单字典）

首版推荐状态集合（仍然保持最小）：

- `Idle`
- `Move`（点地/追击的运动状态）
- `Attack`（普攻状态：命中窗口、可打断、可追击）
- `Evade`（翻滚：位移 + 无敌/减伤窗口 + 冷却/体力）
- `CastSkill`（通用技能状态，可复用同一类实现，通过配置驱动差异）

### 命中判定与反馈

沿用现有 `HitContext / IHitReceiver`，但把“伤害结算”与“反馈”进一步解耦：

- `DamageSystem`：根据技能/武器生成一次伤害（首版可极简）
- `HitReactionSystem`：击退/硬直/受击闪烁（首版可只做闪烁与轻微击退）
- `FloatingText / Sfx`：可选反馈（可关）

### 掉落、拾取、背包与装备（三槽）

新增模块：

- `LootDropSystem`：击杀时根据掉落表生成掉落实体（物品实例）
- `PickupSystem`：拾取范围与交互规则（点击拾取/自动拾取可作为后续开关）
- `InventorySystem`：背包容器
- `EquipmentSystem`：三槽（武器/护甲/饰品）穿戴与卸下
- `ItemRoller`：稀有度与词条抽取（首版词条池小而“有感”）

建议的最小稀有度：

- `Common`（白）
- `Magic`（蓝）
- `Rare`（黄）

### 等级与技能升级（简单可裁剪）

新增：

- `ExperienceSystem`：击杀加经验 -> 升级
- `SkillLevelSystem`：每个技能独立 1-5 级（无技能树面板）

升级给点数规则可先简单：

- 每级 +1 技能点
- 技能点投入到 6 个槽位上的技能（或技能库中的技能）

### 城镇与服务

首版只做最小“服务点”：

- 商人：出售物品换金币（价格简化）
- 治疗/补给：回满生命与体力（或提供免费补给）
- 传送门：野外/地牢 -> 城镇；城镇 -> 最近的野外入口

## 运行时流程

### 点击地面移动

1. 输入层捕获点击地面
2. 意图层产生 `MoveCommand(destination)`
3. `ClickToMoveController` 请求导航路径
4. 逻辑层在 `Move` 状态持续跟随路径点
5. 到达后切回 `Idle`
6. 表现层更新朝向、动画与排序

### 点击怪物自动追击普攻

1. 点击敌人 -> `TargetCommand(target)`
2. `CombatOrchestrator` 进入“追击-攻击”
3. 若距离不足：`ClickToMoveController` 追击目标
4. 进入攻击距离：切换到 `Attack` 状态并触发普攻
5. 命中窗口内启用 Hitbox，分发 `HitContext`
6. 若目标仍存活且仍在追踪范围：继续普攻循环；否则退出到 `Idle/Move`

### 翻滚（Evade）

1. 输入层触发翻滚按钮/手势
2. 若体力足够且冷却就绪：进入 `Evade` 状态
3. 扣体力 -> 位移 -> 短暂无敌/减伤窗口
4. 结束后回到 `Idle/Move` 或继续追击/攻击编排

### 掉落与拾取

1. 敌人死亡：`LootDropSystem` 抽取掉落并生成地面实体
2. 玩家点击掉落或触发拾取：`PickupSystem` 产生 `InteractCommand`
3. `InventorySystem` 收纳；若背包满则反馈
4. 装备界面穿戴到 `EquipmentSystem`，同步属性到角色上下文

### 存档

首版存档键值建议：

- `PlayerLevel` / `Exp`
- `EquipmentSlots`（三槽物品 id + rolled affixes）
- `InventoryItems`
- `Gold`
- `Progress`（例如当前 Act/任务阶段）
- `FeaturesMask`（用于兼容读取）

## 关键设计决策

### “等距”仅作为表现与排序

移动、导航、碰撞仍使用 Godot 2D 体系；等距感由：

- 角色/物体精灵本身的等距绘制
- Y 轴排序（`YSort` / 手动排序）

这样可以避免引入 2.5D 坐标转换与复杂碰撞，且更利于后续裁剪/替换。

### 追击-攻击用编排层，不把规则塞到输入层

“点怪自动追击普攻”是游戏规则，不应该由输入层实现；输入层只产生“我点了谁”。  
追击、到位判定、攻击循环属于 `CombatOrchestrator`，便于测试与替换（例如将来切换到 WASD 也不推翻战斗规则）。

### 技能成长选择“独立 1-5 级”

该方案 UI 成本最低，且可随时升级为技能树。也更符合“系统尽量简单、可裁剪”的约束。

### 三槽装备 + 少量词条

先追求“掉落有意义、换装有感”，而不是装备部位复杂度。词条池控制在小集合，确保每条词条都能立刻改变体感或效率。

## 文件落位建议

以下为建议落位（可在实现计划阶段再细化/校准命名）：

- 输入：
  - `Game/Gameplay/Input/MouseKeyboardInputAdapter.cs`
  - `Game/Gameplay/Input/GamepadInputAdapter.cs`
  - `Game/Gameplay/Input/TouchInputAdapter.cs`
- 目标与移动：
  - `Game/Gameplay/Navigation/ClickToMoveController.cs`
  - `Game/Gameplay/Combat/TargetingService.cs`
  - `Game/Gameplay/Combat/CombatOrchestrator.cs`
- 玩家状态：
  - `Game/Gameplay/Player/States/PlayerEvadeState.cs`
  - `Game/Gameplay/Player/States/PlayerAttackState.cs`（现有扩展）
  - `Game/Gameplay/Player/States/PlayerCastSkillState.cs`
- 资源与配置：
  - `Game/Config/Combat/SkillConfig.cs`
  - `Game/Config/Items/ItemConfig.cs`
  - `Game/Config/Progression/LevelConfig.cs`
  - `Game/Config/GameFeatures.cs`
- 物品与掉落：
  - `Game/Gameplay/Items/InventorySystem.cs`
  - `Game/Gameplay/Items/EquipmentSystem.cs`
  - `Game/Gameplay/Items/LootDropSystem.cs`
- 场景：
  - `Game/Scenes/Town/Town.tscn`
  - `Game/Scenes/World/Act1_Outdoor.tscn`
  - `Game/Scenes/Dungeon/Dungeon01.tscn`

## 测试与验证

### 自动化测试（xUnit）

优先覆盖纯逻辑模块：

- 追击到位判定（距离阈值、停止半径、目标移动重算节奏）
- 攻击编排状态机（有目标/无目标、目标失效、超出追踪范围）
- 体力消耗与回复（翻滚与技能消耗、边界条件）
- 掉落抽取（稀有度概率、词条抽取与去重）

### 手动验证

最小可玩验收清单：

- 鼠标点地：角色能稳定走到目的地并停下
- 鼠标点怪：角色能追到攻击距离并自动普攻直到击杀
- 击杀掉落：地面生成物品，点击能拾取进背包
- 穿戴三槽：穿上后能改变伤害/攻速等关键属性（哪怕先做极简）
- 翻滚：有冷却/体力消耗与明显位移，能打断追击并随后继续追击
- 回城：能进入城镇并售卖物品（哪怕先做最简 UI）

## 扩展前景

在不推翻当前边界的前提下，可自然扩展：

- 章节化 Act 内容、更多地牢/事件点
- 更多敌人类型与技能脚本
- 更多技能与技能符文
- 更细的装备部位与词条池
- 更完整的受击反馈（硬直、击退、击飞、护盾、抗性）
- 更复杂的任务与对话系统

## 结论

本设计优先让项目进入“能玩且像暗黑式 ARPG”的状态：点击移动、追击普攻、技能槽、翻滚体力、掉落与穿戴、回城整理。  
同时通过严格模块边界与功能开关，保证后续可以低成本裁剪或替换系统，而不破坏核心控制链路与工程规范。

