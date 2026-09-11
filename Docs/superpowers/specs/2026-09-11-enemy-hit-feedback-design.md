# 敌人血条与受击反馈设计（表现层）

## 背景

当前敌人被击中只有本体颜色闪烁，死亡是瞬间消失：看不到血量、看不到伤害数字、没有位移反馈。伤害/抗性体系与精英/Boss 落地后，战斗缺少「我的攻击有多大效果」的直观呈现。本切片补齐敌人侧的表现层。

## 目标

- **血条**：敌人头顶世界空间血条（深色底 + 红色填充）；满血隐藏、受伤显示；Boss 血条更宽
- **伤害数字**：每次受击在敌人位置弹出实际伤害数字，向上漂浮并淡出后自毁
- **击退**：受击时沿攻击方向获得小幅位移冲量，随时间指数衰减（Boss 冲量 ×0.3，体现重量感）
- **死亡反馈**：死亡不再瞬间消失——立即结算（信号/掉落/经验时序不变），本体碰撞关闭、缩放渐隐约 0.18 秒后释放
- 纯逻辑可测：`KnockbackModel`（冲量施加与指数衰减）、`EnemyFeedbackRules`（血条显隐规则）

## 非目标

- 玩家侧受击反馈（屏幕震动/红闪）
- 暴击数字样式、数字合并/上限
- 敌人血条数字文本、精英词缀名飘字
- 音效与粒子

## 方案比选

- **方案 A（采用）**：血条用自定义 `_Draw` 的 `Node2D` 挂在敌人场景；伤害数字用代码生成的 `Label` + `Tween`；击退用纯逻辑模型 + 胶水叠加速度。
- 方案 B：`TextureProgressBar` 血条（需贴图资源）。项目当前无美术资源，自绘最贴合像素占位风格。
- 方案 C：击退做成位移补间（Tween 位移）。与 AI 的 `MoveAndSlide` 移动互相打架，速度叠加衰减更稳。

## 模块设计

### 纯逻辑（`Game/Gameplay/Enemies/Feedback/`）

- `KnockbackModel`：
  - `float X, Y`（当前冲量速度，Godot 无关）
  - `Apply(float dirX, float dirY, float impulse)`：方向 × 冲量写入（覆盖式，连续受击不叠加成失控）
  - `Tick(float delta)`：按 `DecayRate`（默认 8/秒）指数衰减
- `EnemyFeedbackRules.ShowHealthBar(int hp, int maxHp)`：`maxHp > 0 && hp < maxHp`

### 表现层

- `Game/Scenes/Enemies/EnemyHealthBar.cs`（挂进 `BasicEnemy.tscn`，位置头顶）：
  - `Update(int hp, int maxHp, float width)`：更新并 `QueueRedraw`
  - `_Draw()`：深色底 + 红色填充（宽度按血量比例），`Visible` 由规则控制
- `Game/Scenes/Enemies/DamageNumber.cs`：`Label` 子类；静态 `Spawn(Node parent, Vector2 pos, int damage)` 生成，Tween 上浮 24px/0.6s + 淡出，完成后自毁
- `BasicEnemyController`：
  - `ReceiveHit`：结算后 `Update` 血条、`DamageNumber.Spawn`、`_knockback.Apply(hit.Direction, 冲量)`（Boss ×0.3）
  - `_PhysicsProcess`：AI 速度 + 击退速度合成后 `MoveAndSlide`
  - 死亡分支：信号照发（时序不变）→ 关碰撞（`SetDeferred`）→ 隐藏血条 → 本体缩放渐隐 0.18s → 释放；期间 `_dying` 屏蔽 AI/触碰/重复受击

## 测试计划

- `KnockbackModelTests`：Apply 写入方向速度；Tick 指数衰减单调下降；持续 Tick 趋近 0；覆盖式 Apply 不叠加
- `EnemyFeedbackRulesTests`：满血隐藏；受伤显示；`maxHp ≤ 0` 隐藏；0 血仍显示（空条，随后由死亡动画隐藏）
- 胶水以构建 + 引擎冒烟兜底：受击后血条可见、伤害数字存在、死亡不再瞬间消失

## 可裁剪性

- 纯表现层：不触碰任何规则系统与存档；血条/数字/击退互相独立，可单独移除
