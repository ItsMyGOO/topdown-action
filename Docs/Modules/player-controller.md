# Player Controller

## 角色

`PlayerController` 是玩家角色的 Godot 运行时入口。它挂在 `CharacterBody2D` 上，负责把输入、状态机、移动规则和表现同步串起来。

## 每帧流程

当前 `_PhysicsProcess()` 中的主流程如下：

1. 从 `PlayerInputAdapter` 读取输入
2. 生成并更新 `ActorIntent`
3. 将意图写入 `ActorContext`
4. 由 `ActorMotor` 根据配置计算目标速度
5. 根据当前是否存在移动意图切换 `Idle` / `Move`
6. 调用 `MoveAndSlide()`
7. 将结果同步给 `PlayerView`

## 相关模块

- `PlayerInputAdapter`
  读取 Godot 输入映射中的 `move_left`、`move_right`、`move_up`、`move_down`
- `ActorIntent`
  表达当前帧角色控制意图
- `ActorContext`
  保存当前运行时状态，例如意图、速度、朝向和移动许可
- `ActorMotor`
  负责计算加速、减速和最大速度
- `PlayerIdleState` / `PlayerMoveState`
  负责当前首版的状态更新
- `PlayerView`
  负责把朝向等逻辑结果同步到表现节点

## 为什么这样拆

这样拆分的目的，是避免把所有角色逻辑都堆到一个 Godot 脚本里。拆分后：

- 更容易测试纯逻辑模块
- 更容易让怪物或 NPC 复用 `Actor` 级别的基础设施
- 更容易替换输入来源，例如手柄或未来的移动端虚拟摇杆
- 更容易把动画层改成更复杂的表现，而不影响移动逻辑

## 当前限制

当前 `PlayerView` 只同步朝向旋转，主要用来证明“逻辑到表现”的链路已经接通。后续接动画树时，推荐继续让 `PlayerView` 成为唯一表现写入口。
