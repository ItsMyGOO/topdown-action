# 第五期：世界等级、地下城与传送点网络

## 背景

当前只有一个固定难度的世界场景（11 怪 + 1 Boss）与城镇/世界两个传送点。玩家没有难度选择、没有可挑战的进阶内容。世界等级（World Tier）是 D4 的难度骨架，地下城是刷装备的核心场景。

本切片实现难度分层与实例化地下城入口，并与既有精英/Boss/词缀系统联动。**仓库**体量较大，拆为下一独立切片。

## 目标

- **世界等级**（纯逻辑 `WorldTierModel`）：
  - 3 档：`Normal(1) / Nightmare(2) / Hell(3)`，每档敌人生命 ×1/×2.5/×5、伤害 ×1/×1.8/×2.6、经验 ×1/×1.75/×2.5、掉落稀有度下限 Common/Rare/Rare
  - `GameSession.WorldTier`（默认 Normal）；存档 `SaveData.WorldTier` 持久化，旧档默认 1
  - 生效路径：`WorldRoot` 生成期把等级倍率并入 `ElitePlan`（`ApplyPlan` 前）与经验发放；城镇石像循环切换（与职业石像同交互模式），HUD 显示当前等级名
- **地下城**：
  - `Dungeon.tscn`（独立场景）：密敌人（18 只）、3 只固定 Boss、无回城传送门；`WorldRoot.cs` 复用（新增 `[Export] DungeonMode`：敌人更多/Boss 更少密度/经验 ×1.5/掉落稀有度下限 Rare/清完全部 Boss 出现回城门）
  - 敌人生成改为代码铺设（`DungeonMode` 下运行时实例化 18+3），World 场景保持手工摆放
- **传送点网络**：
  - 城镇现有 `PortalToWorld` 保留；新增 `PortalToDungeon`（进入地下城）
  - 地下城回城门：全部 Boss 死亡后生成/激活，走交互回城
- HUD：世界等级标签（普通/噩梦/地狱着色区分）

## 非目标

- 仓库（下一切片）
- 难度解锁条件（D4 要求通关前置；这里全部开放，单机调试语义）
- 地下城布局随机化、地砖变化、专属词缀机制

## 方案比选

- **方案 A（采用）**：`WorldTierModel` 纯数值表 + `WorldRoot` 参数化双模式。地下城复用全部敌人/掉落/经验链路，只换铺设数据。
- 方案 B：地下城做独立场景类。重复实现掉落/AI/精英逻辑，违背复用。
- 方案 C：世界等级做词条式缩放。数值表更直观可测。

## 模块设计

### 纯逻辑（`Game/Gameplay/Progression/Tiers/`）

- `WorldTierDefinition(int Tier, string Name, float EnemyHpMultiplier, float EnemyDamageMultiplier, float XpMultiplier, ItemRarity DropRarityFloor)`
- `WorldTierDatabase.Catalog`（3 条）+ `Get(tier)`（越界钳制）
- 存档：`SaveData.WorldTier`（int），mapper 往返，越界回退 1

### 胶水

- `GameSession.WorldTier`；`WorldRoot` 新增 `[Export] bool DungeonMode`、`[Export] float DungeonXpMultiplier = 1.5f`
- `WorldRoot._Ready` 敌人铺设（Dungeon 模式）：运行时实例化 18 普通怪 + 3 Boss（网格布点）；Boss 死亡计数 → 全灭后 `CallDeferred` 激活回城门
- 难度并入：`ApplyPlan` 前把 `WorldTier` 倍率与精英计划合成（生命/伤害相乘）；经验 = 基础 × 装备 × 天赋 × 等级 × 地下城；掉落 `RarityFloor` 取 max(精英计划, 世界等级, 地下城)
- `Town.tscn` 加 `PortalToDungeon`；`TownController` 交互切换场景（地下城场景路径）
- HUD `TierLabel`：显示「世界等级: 普通」等，地狱档红字

## 测试计划

- `WorldTierDatabaseTests`：3 档、数值矩阵、Get 越界钳制、名称正确
- `GameSessionSaveTests`（扩展）：WorldTier 往返、旧档默认 1、越界值回退
- 引擎冒烟：切地狱档后敌人生命/伤害按倍率变化；地下城场景铺设 21 敌且含 3 Boss；杀光 Boss 后回城门出现

## 可裁剪性

- 世界等级默认 Normal（乘数全 1 语义）；地下城是独立场景与开关，不影响世界玩法
