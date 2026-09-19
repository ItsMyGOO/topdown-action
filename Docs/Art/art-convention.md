# 美术规范（以骑士素材包为基准）

## 素材事实（实测）

- 位置：`Game/Art/2D HD Character Knight/Spritesheets/With shadows/`
- 单张表 1920×1024，网格 **16 列 × 10 行，帧 120×102**
- 每张表 = 一个动作（Idle/Run/Walk/Melee/CastSpell/TakeDamage/Die/Rolling/…），多行是该动作的方向/变体行
- 实际列簇 15（Idle）/31（Run，含重叠簇）——**以 16 列网格 + 每动作配置实际帧数为准**，空帧跳过

## 美术规范

1. **命名**：`Game/Art/<风格>/<角色或物>/<动作>.png`，一张表一个动作
2. **网格**：整表等分网格；帧尺寸记录在 `AnimationCatalog`，不靠猜
3. **动画映射**：`AnimationCatalog`（纯逻辑）定义 动作→(贴图, 行, 帧数, fps, loop)
4. **方向处理**：该包是单方向（右向）+ 8 向斜 row 变体；项目用「左右翻转 + 上下选行」策略映射
5. **像素风约束**：当前骑士素材是 HD 手绘，导入后缩放至世界尺寸（scale 0.5）并保持 nearest 过滤；真像素风资产替换时同规格同名即可

## 落地

- `AnimationCatalog`：Idle(15f/8fps loop)、Run(15f/12fps loop)、Melee(即 AttackTake? 用 Melee 表 10f/12fps once)、CastSpell(10f/12fps once)、TakeDamage(6f/10fps once)、Die(10f/8fps once)
- PlayerView 改为 `Sprite2D + hframes/vframes + FrameCoords`，由状态机驱动动画名
- 敌人暂保持现有像素史莱姆（骑士包仅玩家），下一批素材到位后套同一规范
