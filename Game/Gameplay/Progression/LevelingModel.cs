using System;

namespace GodotGameTemplate.Gameplay.Progression;

/// <summary>
/// 一次 <see cref="LevelingModel.AddXp"/> 调用的结果。
/// </summary>
public readonly record struct LevelUpResult(int LevelsGained, int NewLevel);

/// <summary>
/// 等级/经验模型（纯逻辑）：
/// <para>
/// - 维护当前等级（<see cref="Level"/>）与等级内已积累经验（<see cref="CurrentXp"/>）
/// - 经验满自动升级，支持一次跨越多级并结转溢出
/// - 提供按级成长的加成记账（<see cref="ApplyGrowth"/>），保证加成不重复发放
/// </para>
/// <para>
/// 注意：该类型不依赖 Godot API，便于单元测试与复用。
/// </para>
/// </summary>
public sealed class LevelingModel
{
    /// <summary>
    /// 每级基础经验需求（1 级升 2 级所需），升级曲线的基础值。
    /// </summary>
    public const int BaseXpPerLevel = 50;

    /// <summary>
    /// 每级增加的最大法力加成。
    /// </summary>
    public const float BonusManaPerLevel = 2f;

    /// <summary>
    /// 每级增加的最大体力加成。
    /// </summary>
    public const float BonusStaminaPerLevel = 2f;

    /// <summary>
    /// 当前等级，从 1 开始，单调递增。
    /// </summary>
    public int Level { get; private set; } = 1;

    /// <summary>
    /// 当前等级内已积累的经验。
    /// </summary>
    public int CurrentXp { get; private set; }

    /// <summary>
    /// 成长加成已经应用到的等级（用于增量记账，避免重复加成）。
    /// </summary>
    public int AppliedGrowthLevel { get; private set; } = 1;

    /// <summary>
    /// 升到下一级所需的经验（随等级增长）。
    /// </summary>
    public int XpToNextLevel => BaseXpPerLevel * Level;

    /// <summary>
    /// 增加经验；负数与零忽略。经验达到阈值时自动升级并结转溢出。
    /// </summary>
    public LevelUpResult AddXp(int amount)
    {
        if (amount <= 0)
        {
            return new LevelUpResult(0, Level);
        }

        CurrentXp += amount;

        var levelsGained = 0;
        while (CurrentXp >= XpToNextLevel)
        {
            CurrentXp -= XpToNextLevel;
            Level++;
            levelsGained++;
        }

        return new LevelUpResult(levelsGained, Level);
    }

    /// <summary>
    /// 从存档恢复等级与经验。
    /// <para>
    /// 成长记账重置为 1 级：资源模型的 Max 不随存档持久化，
    /// 加成应在外部重新调用 <see cref="ApplyGrowth"/> 结算。
    /// </para>
    /// </summary>
    public void Restore(int level, int xp)
    {
        Level = Math.Max(1, level);
        CurrentXp = Math.Max(0, xp);
        AppliedGrowthLevel = 1;
    }

    /// <summary>
    /// 把尚未应用的等级加成增量应用到传入的资源模型。
    /// <para>
    /// 允许传 <c>null</c> 跳过其一（例如法力由会话侧应用、体力由玩家绑定侧应用）。
    /// 内部记账保证同一批等级的加成不会被重复发放。
    /// </para>
    /// </summary>
    public void ApplyGrowth(StaminaModel? stamina, ManaModel? mana)
    {
        var pendingLevels = Level - AppliedGrowthLevel;
        if (pendingLevels <= 0)
        {
            return;
        }

        if (stamina != null)
        {
            stamina.Max += BonusStaminaPerLevel * pendingLevels;
        }

        if (mana != null)
        {
            mana.Max += BonusManaPerLevel * pendingLevels;
        }

        AppliedGrowthLevel = Level;
    }
}
