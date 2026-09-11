using System;
using System.Collections.Generic;
using System.Linq;

namespace GodotGameTemplate.Gameplay.Progression.Talents;

/// <summary>
/// 天赋盘状态（纯逻辑）：
/// <para>
/// - 维护各天赋当前等级；可用点数 = 等级 - 1 - 已花点数（钳 0）
/// - 分配校验：天赋存在、未满级、点数可用、前置达标
/// </para>
/// </summary>
public sealed class TalentModel
{
    private readonly Dictionary<string, int> _ranks = [];

    /// <summary>
    /// 各天赋当前等级（只读视图，只包含已分配的天赋）。
    /// </summary>
    public IReadOnlyDictionary<string, int> Ranks => _ranks;

    /// <summary>
    /// 已花费的天赋点总数。
    /// </summary>
    public int PointsSpent => _ranks.Values.Sum();

    /// <summary>
    /// 在给定角色等级下的可用点数。
    /// </summary>
    public int AvailablePoints(int level)
    {
        return Math.Max(0, level - 1 - PointsSpent);
    }

    /// <summary>
    /// 是否可以把一点投入指定天赋。
    /// </summary>
    public bool CanAllocate(string id, int level)
    {
        var definition = TalentDatabase.Get(id);
        if (definition == null)
        {
            return false;
        }

        if (_ranks.TryGetValue(id, out var rank) && rank >= definition.MaxRank)
        {
            return false;
        }

        if (AvailablePoints(level) <= 0)
        {
            return false;
        }

        if (definition.RequiresId != null)
        {
            var prerequisiteRank = _ranks.GetValueOrDefault(definition.RequiresId);

            return prerequisiteRank >= definition.RequiresRank;
        }

        return true;
    }

    /// <summary>
    /// 尝试投入一点；校验失败返回 false 且无副作用。
    /// </summary>
    public bool Allocate(string id, int level)
    {
        if (!CanAllocate(id, level))
        {
            return false;
        }

        _ranks[id] = _ranks.GetValueOrDefault(id) + 1;
        return true;
    }

    /// <summary>
    /// 从存档恢复；未知天赋与非法等级（≤0 或超上限）做钳制/忽略。
    /// </summary>
    public void Restore(IEnumerable<(string Id, int Rank)> ranks)
    {
        ArgumentNullException.ThrowIfNull(ranks);

        _ranks.Clear();

        foreach (var (id, rank) in ranks)
        {
            var definition = TalentDatabase.Get(id);
            if (definition == null || rank <= 0)
            {
                continue;
            }

            _ranks[id] = Math.Min(rank, definition.MaxRank);
        }
    }
}
