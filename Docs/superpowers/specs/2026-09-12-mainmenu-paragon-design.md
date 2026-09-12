# 第六期：角色创建主菜单 与 简化版 Paragon

## 背景

两个遗留大项：游戏没有正式入口（直接进城镇，职业靠调试石像切换）；50 级后的长线成长缺失（D4 的 Paragon 板按用户要求做简化版）。

## 切片 A：角色创建主菜单

### 目标

- `MainMenu.tscn` 成为启动场景（`project.godot run/main_scene`）：
  - **开始新游戏**：进入职业选择（三职业卡片，显示名称/倾向/倍率）→ 选定后 `GameSession.NewGame(classId)` 重置全部会话状态并进城镇
  - **继续游戏**：有存档时显示（直接读档进城镇）；无存档置灰
  - **退出**
- `GameSession.NewGame(classId)`（纯逻辑，可测）：清空金币/背包/仓库/装备/经验（Restore 1,0）/天赋/药水补满/世界等级回 1，写入职业
- 调试石像保留（运行中切换），但正式流程走主菜单
- 死亡回城与继续游戏共用城镇入口，行为不变

### 非目标

- 多存档槽位（单档维持现状）
- 角色外观随职业变化（美术期）

## 切片 B：简化版 Paragon

### 目标（按用户「简化版」要求，不做 2D 网格/旋转/雕纹辐射）

- **解锁条件**：角色等级 ≥ 10 后，每升 1 级 +1 Paragon 点（`ParagonModel.PointsFromLevel(level)`）
- **四系加成**（线性、无上限，每点）：
  | 类别 | Id | 每点效果 |
  |------|----|----------|
  | 暴虐 | brutality | +1 伤害 |
  | 活力 | vitality | +3 最大生命 |
  | 智谋 | cunning | +1% 经验 |
  | 迅捷 | alacrity | +0.5% 移速 |
- `ParagonModel`（纯逻辑）：字典分配 + 可用点数（`PointsFromLevel - 已花`）+ 校验 + Restore；`ParagonStats.Aggregate` 输出乘区/平加
- 生效并入 `PlayerController` 结算（伤害/生命）与 `WorldRoot`（经验）、`ClickToMove`（移速）
- 存档：`SaveData.Paragon`（同天赋格式），旧档缺省空
- UI：`ParagonPanel`（复用天赋面板模式，P 键开关 `open_paragon` 输入映射），显示解锁状态（< 10 级提示）

### 非目标

- 网格棋盘、旋转分支、雕纹石与辐射半径、板与板连接
- 重置洗点

## 模块设计

- `Game/UI/MainMenu/`：MainMenu.cs/.tscn（脚本控制按钮状态与场景切换）
- `Game/Gameplay/Progression/Paragon/`：`ParagonCategory` 枚举、`ParagonModel`、`ParagonStats`
- `Game/UI/Paragon/`：ParagonPanel.cs/.tscn

## 测试计划

- `GameSessionNewGameTests`：NewGame 后各状态归零/满、职业写入、继续游戏不重置
- `ParagonModelTests`：解锁前 0 点；10 级 1 点逐级递增；分配/上限无（线性）；点数不足拒绝；Restore
- `ParagonStatsTests`：空全零；分配后乘区平加正确
- 存档：Paragon 往返、旧档缺省
- 引擎冒烟：主菜单场景按钮存在；NewGame 后进城镇状态干净；Paragon 分配后伤害/生命变化并持久化

## 可裁剪性

- 主菜单只是入口层；Paragon 独立模型与开关输入，P 键不按即不存在
