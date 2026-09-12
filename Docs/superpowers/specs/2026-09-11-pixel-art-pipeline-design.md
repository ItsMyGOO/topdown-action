# 第三期：2D 像素美术管线（角色序列帧 + 瓦片地面）

## 背景

角色/敌人/地面全部是占位多边形；玩家视角朝向靠「旋转整个视图节点」表达——与序列帧动画体系不兼容。目标要求美术全面转为 2D 像素，因此本期建立**可再生的程序化像素美术管线**（无外部素材依赖），并把玩家/敌人/地面替换为像素渲染。

## 目标

- **程序化资产生成**：`Tools/PixelArtGenerator.gd`（Godot 无头脚本）生成并落盘：
  - `Game/Art/Pixel/player_sheet.png`：24×24 帧、4 列（行走帧）× 4 行（下/上/左/右）的小人序列帧
  - `Game/Art/Pixel/enemy_sheet.png`：16×16 × 4 帧的怪物序列帧（弹跳挤压）
  - `Game/Art/Pixel/tileset.png`：3 枚 32×32 地面瓦片（草地两变体 + 泥土）
  - 脚本入库，改参数重跑即可再生成
- **像素渲染设置**：项目默认纹理过滤改为 Nearest（`rendering/textures/canvas_textures/default_texture_filter=0`）
- **玩家表现**：`PlayerView` 由「旋转多边形」改为序列帧动画——`FacingAnimationResolver`（纯逻辑）把朝向+移动状态映射到行/帧；行走用 8fps 四帧循环，静止用 0 帧
- **敌人表现**：`BasicEnemy.tscn` 的多边形 Body 换成序列帧 Sprite；控制器导出类型改为 `Sprite2D`（染色/缩放/死亡动画改走 Modulate，Elite 词缀色调不变）
- **瓦片地面**：`WorldRoot` 运行时创建 `TileMapLayer` 铺满世界（20×12 格 × 32px = 640×384，与相机边界对齐）；格子变体由 `GroundLayout`（纯逻辑，种子确定性）生成
- 存档/规则系统零改动（纯表现层）

## 非目标

- 真实手绘像素资产、Aseprite 工作流（管线就绪后可直接替换同名 PNG）
- 8 向序列帧（先 4 向 + 翻转扩展位）、攻击/受击专属帧
- 瓦片碰撞/遮挡层、动画 TileSet

## 方案比选

- **方案 A（采用）**：单张 spritesheet + `Sprite2D`（hframes/vframes）+ 代码切帧；TileMapLayer 运行时铺地面。
- 方案 B：`AnimatedSprite2D` + `SpriteFrames` 资源。资源文件里要为每帧建 AtlasTexture，20+ 条目全手写，维护成本高于数字切帧。
- 方案 C：外部美术工具链（Aseprite 导出脚本）。当前无素材无工具，程序化生成先行，管线接口（同名 PNG）保留。

## 模块设计

### 纯逻辑（`Game/Gameplay/Common/Pixel/`）

- `FacingAnimationResolver.Resolve(Vector2 facing, bool moving) → PixelAnim`：
  - `PixelAnim(string RowName, int Row, int Frame)`；行：下=0/上=1/左=2/右=3（主轴判定，x 优先）
  - moving=true 用行走循环帧，false 固定 0 帧
- `WalkCycle.FrameFor(float timeSeconds, float fps = 8f, int frameCount = 4)`：`(int)(t*fps) % count`
- `GroundLayout.VariantAt(int x, int y, int seed, int variants)`：整型哈希 → 确定性变体索引；`Generate(width,height,seed,variants)` 返回全量网格

### 资产生成（`Tools/PixelArtGenerator.gd`）

- 继承 `SceneTree`，`--headless -s` 运行；`Image.set_pixel` 逐像素绘制后 `save_png`
- 调色板：皮肤/布衣/头发/描边；怪物血红+眼白；草地两绿+泥棕

### 表现接线

- `PlayerView`：`Sync(context, delta)` 累计时钟 → 行列写入 `Sprite.FrameCoords`；`Player.tscn` View 下挂 `Sprite`（sheet + hframes/vframes=4）
- `BasicEnemyController`：`Body` 导出类型 `Polygon2D` → `Sprite2D`；染色改 `Modulate`（HitFlash 默认过曝白 `Color(3,3,3)`）；追击时走 4 帧循环，空闲 0 帧
- `WorldRoot._Ready`：创建 `TileMapLayer`（`Game/Art/Pixel/tileset.tres`），按 `GroundLayout` 铺 20×12
- `project.godot`：nearest 过滤

## 测试计划

- `FacingAnimationResolverTests`：主轴方向（下/上/左/右）、对角线取 x 优先、零向量默认下、移动/静止帧
- `WalkCycleTests`：0 秒→0 帧；循环回绕；fps 影响推进
- `GroundLayoutTests`：同种子同结果；变体在界内；网格存在多种变体
- 引擎冒烟：世界内有铺满的 TileMapLayer；玩家/敌人 Sprite 帧数与贴图非空

## 可裁剪性

- 表现层独立：替换回多边形只需还原 `PlayerView`/`BasicEnemy.tscn` 两处
