using System;
using System.Collections.Generic;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 最小背包模型：固定 3 槽，按顺序放入第一个空槽。
/// <para>
/// 纯逻辑，不依赖 Godot。
/// </para>
/// </summary>
public sealed class Inventory3
{
    private readonly ItemStack?[] _slots = new ItemStack?[3];

    public IReadOnlyList<ItemStack?> Slots => _slots;

    /// <summary>
    /// 尝试把物品栈放入背包第一个空槽。
    /// </summary>
    public bool TryAdd(ItemStack stack)
    {
        for (var i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                continue;
            }

            _slots[i] = stack;
            return true;
        }

        return false;
    }
}
