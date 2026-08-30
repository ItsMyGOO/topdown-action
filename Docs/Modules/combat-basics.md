# Combat Basics

当前战斗模块只实现首个最小闭环：

- 玩家普通近战攻击
- 短时 Hitbox 命中判定
- 通用命中接收接口
- 训练假人反馈

## 责任边界

- `PlayerAttackState`：管理攻击生命周期、朝向锁定和命中窗口
- `PlayerAttackHitbox`：负责场景中的命中检测、目标去重和受击分发
- `IHitReceiver`：定义目标如何接收一次命中
- `TrainingDummyController`：作为最小受击目标验证命中闭环

## 训练假人命中接线

训练假人的 `Hurtbox` 是子 `Area2D`，实际受击逻辑由父节点 `TrainingDummyController` 实现。
`PlayerAttackHitbox` 在命中 `Area2D` 后会沿节点父链向上查找 `IHitReceiver`，这样 `Hurtbox` 只负责被检测，反馈逻辑仍集中在控制脚本中。
