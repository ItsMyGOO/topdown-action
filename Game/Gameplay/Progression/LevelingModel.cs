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
    /// 资源模型的基准 Max（与 <see cref="ManaModel"/> 默认值一致）。
    /// </summary>
    public const float BaseManaMax = 100f;

    /// <summary>
    /// 生命模型的基准 Max。
    /// </summary>
    public const float BaseHealthMax = 100f;

    /// <summary>
    /// 每级增加的最大生命加成。
    /// </summary>
    public const float BonusHealthPerLevel = 5f;

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
    /// 外部来源的最大法力加成（如装备词条）。由结算方在 <see cref="ApplyGrowth"/> 前写入。
    /// </summary>
    public float ExternalManaBonus { get; set; }

    /// <summary>
    /// 外部来源的最大生命加成（如天赋）。由结算方在 <see cref="ApplyGrowth"/> 前写入。
    /// </summary>
    public float ExternalHealthBonus { get; set; }

    /// <summary>
    /// 升到下一级所需的经验（随等级增长）。
    /// </summary>
    public int XpToNextLevel => BaseXpPerLevel * Level;

    /// <summary>
    /// 期望最大法力（基准 + 等级加成 + 外部加成），供结算方判断是否需要重新应用。
    /// </summary>
    public float DesiredManaMax =>
        BaseManaMax + BonusManaPerLevel * (Level - 1) + ExternalManaBonus;

    /// <summary>
    /// 期望最大生命（基准 + 等级加成 + 外部加成）。
    /// </summary>
    public float DesiredHealthMax =>
        BaseHealthMax + BonusHealthPerLevel * (Level - 1) + ExternalHealthBonus;

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
    /// 把等级加成与外部加成（装备词条/天赋）合并应用到传入的资源模型
    /// （绝对式赋值：基准 + 每级加成×已达等级 + 外部加成）。
    /// <para>
    /// 绝对式赋值天然幂等：读档后（<see cref="Restore"/>）重复结算不会叠加加成。
    /// 允许传 <c>null</c> 跳过其一；生命模型结算后会 <see cref="HealthModel.ClampToMax"/>。
    /// </para>
    /// </summary>
    public void ApplyGrowth(ManaModel? mana, HealthModel? health = null)
    {
        var gainedLevels = Level - 1;

        if (mana != null)
        {
            mana.Max = BaseManaMax + BonusManaPerLevel * gainedLevels + ExternalManaBonus;
        }

        if (health != null)
        {
            health.Max = BaseHealthMax + BonusHealthPerLevel * gainedLevels + ExternalHealthBonus;
            health.ClampToMax();
        }

        AppliedGrowthLevel = Level;
    }
}
