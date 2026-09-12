using System;
using System.Collections.Generic;
using System.Linq;

namespace GodotGameTemplate.Gameplay.Progression.Paragon;

/// <summary>
/// Paragon 加成类别（简化版：线性无上限）。
/// </summary>
public enum ParagonCategory
{
    /// <summary>暴虐：每点 +1% 伤害。</summary>
    Brutality,

    /// <summary>活力：每点 +3 最大生命。</summary>
    Vitality,

    /// <summary>智谋：每点 +1% 经验。</summary>
    Cunning,

    /// <summary>迅捷：每点 +0.5% 移速。</summary>
    Alacrity,
}

/// <summary>
/// 简化版 Paragon 模型（纯逻辑）：
/// <para>
/// - 解锁：等级 ≥ <see cref="UnlockLevel"/>，此后每级 +1 点
/// - 无上限线性加成，无洗点
/// </para>
/// </summary>
public sealed class ParagonModel
{
    /// <summary>
    /// 解锁等级。
    /// </summary>
    public const int UnlockLevel = 10;

    private readonly Dictionary<ParagonCategory, int> _ranks = [];

    /// <summary>
    /// 各类别已分配点数（只含已分配项）。
    /// </summary>
    public IReadOnlyDictionary<ParagonCategory, int> Ranks => _ranks;

    /// <summary>
    /// 已花总点数。
    /// </summary>
    public int TotalSpent => _ranks.Values.Sum();

    /// <summary>
    /// 指定等级可获得的 Paragon 点总数。
    /// </summary>
    public static int PointsFromLevel(int level)
    {
        return Math.Max(0, level - UnlockLevel + 1);
    }

    /// <summary>
    /// 指定等级下的可用点数（钳 0）。
    /// </summary>
    public int AvailablePoints(int level)
    {
        return Math.Max(0, PointsFromLevel(level) - TotalSpent);
    }

    /// <summary>
    /// 尝试投一点；未解锁或点数不足返回 false。
    /// </summary>
    public bool Allocate(ParagonCategory category, int level)
    {
        if (AvailablePoints(level) <= 0)
        {
            return false;
        }

        _ranks[category] = _ranks.GetValueOrDefault(category) + 1;

        return true;
    }

    /// <summary>
    /// 从存档恢复。
    /// </summary>
    public void Restore(IEnumerable<(ParagonCategory Category, int Rank)> ranks)
    {
        ArgumentNullException.ThrowIfNull(ranks);

        _ranks.Clear();

        foreach (var (category, rank) in ranks)
        {
            if (rank > 0)
            {
                _ranks[category] = rank;
            }
        }
    }
}
