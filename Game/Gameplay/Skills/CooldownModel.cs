using System;
using System.Collections.Generic;
using System.Linq;
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Skills;

/// <summary>
/// 冷却模型（纯逻辑）：
/// <para>
/// - 按技能槽位维护剩余冷却时间（秒）
/// - 提供启动冷却与按帧推进（<see cref="Tick"/>）
/// </para>
/// <para>
/// 注意：该类型不依赖 Godot API，便于单元测试与复用。
/// </para>
/// </summary>
public sealed class CooldownModel
{
    private readonly Dictionary<SkillSlot, float> _remaining = new();

    /// <summary>
    /// 获取指定技能槽位的剩余冷却时间（秒）。
    /// </summary>
    public float GetRemaining(SkillSlot slot)
    {
        return _remaining.TryGetValue(slot, out var value) ? value : 0f;
    }

    /// <summary>
    /// 指定技能槽位是否处于可用状态（冷却归零）。
    /// </summary>
    public bool IsReady(SkillSlot slot)
    {
        return GetRemaining(slot) <= 0f;
    }

    /// <summary>
    /// 启动指定技能槽位的冷却。
    /// </summary>
    /// <param name="slot">技能槽位。</param>
    /// <param name="seconds">冷却时长（秒）。</param>
    public void Start(SkillSlot slot, float seconds)
    {
        _remaining[slot] = MathF.Max(0f, seconds);
    }

    /// <summary>
    /// 推进所有技能槽位的冷却计时。
    /// </summary>
    public void Tick(float delta)
    {
        if (delta <= 0f || _remaining.Count == 0)
        {
            return;
        }

        // 避免边迭代边修改：复制 key 列表后再更新。
        var keys = _remaining.Keys.ToArray();
        foreach (var key in keys)
        {
            var next = MathF.Max(0f, _remaining[key] - delta);
            if (next <= 0f)
            {
                _remaining.Remove(key);
            }
            else
            {
                _remaining[key] = next;
            }
        }
    }
}
