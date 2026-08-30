# Overall Architecture

## 目标

本项目当前首个核心切片的目标，是为俯视角 2D 像素动作游戏建立一套可持续演进的角色控制基础设施。它优先保证以下几点：

- 角色八方向连续移动
- 商业项目可维护性
- 可在不同项目之间复用的模块边界
- 为后续战斗、交互、敌人行为扩展保留接口

## 分层

当前角色控制主链路遵循：

`输入 -> 意图 -> 逻辑 -> 表现`

各层职责如下：

- 输入层：读取 Godot 输入系统中的设备输入
- 意图层：把设备输入转换成统一的角色控制意图
- 逻辑层：负责状态切换、移动规则和运行时上下文
- 表现层：把逻辑结果同步到可视节点，例如朝向或动画参数

## 依赖方向

依赖应该单向流动：

`PlayerInputAdapter -> ActorIntent -> ActorContext / StateMachine / ActorMotor -> PlayerView`

这里的关键点是：

- 逻辑层不直接关心输入设备类型
- 表现层不拥有游戏规则
- 状态机和移动规则围绕 `Actor` 抽象，而不是只绑定玩家

## 目录约定

- `Game/Gameplay/Actors/`：角色级通用数据与移动逻辑
- `Game/Gameplay/Common/StateMachine/`：可复用的轻量状态机
- `Game/Gameplay/Input/`：输入适配与意图提供接口
- `Game/Gameplay/Player/`：玩家专属控制器、状态与表现桥接
- `Game/Config/Player/`：玩家参数资源
- `Game/Scenes/`：Godot 场景资源
- `Docs/`：项目文档

## 当前非目标

当前阶段不覆盖以下内容：

- 完整战斗系统
- 敌人 AI
- 存档
- 关卡流程
- 移动端虚拟按键具体 UI

这些内容会建立在当前角色控制框架之上，而不是与当前实现混写。
