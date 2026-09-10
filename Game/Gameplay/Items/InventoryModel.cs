using System;
using System.Collections.Generic;
using System.Linq;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 背包模型（纯逻辑，可单测）。
/// </summary>
public sealed class InventoryModel
{
    private readonly List<ItemInstance> _items = new();

    /// <summary>
    /// 背包容量（首版默认 24）。
    /// </summary>
    public int Capacity { get; set; } = 24;

    /// <summary>
    /// 物品列表（只读视图）。
    /// </summary>
    public IReadOnlyList<ItemInstance> Items => _items;

    /// <summary>
    /// 尝试加入一件物品。背包满则返回 false。
    /// </summary>
    public bool TryAdd(ItemInstance item)
    {
        if (_items.Count >= Capacity)
        {
            return false;
        }

        _items.Add(item);
        return true;
    }

    /// <summary>
    /// 移除一件物品。
    /// </summary>
    public bool Remove(ItemInstance item)
    {
        return _items.Remove(item);
    }

    /// <summary>
    /// 清空背包。
    /// </summary>
    public void Clear()
    {
        _items.Clear();
    }

    /// <summary>
    /// 用一整包物品替换当前背包内容。
    /// </summary>
    public void ReplaceItems(IEnumerable<ItemInstance> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var nextItems = items.ToList();
        if (nextItems.Count > Capacity)
        {
            throw new InvalidOperationException(
                $"Inventory capacity is {Capacity}, but received {nextItems.Count} items."
            );
        }

        _items.Clear();
        _items.AddRange(nextItems);
    }
}
