using System.Collections.Generic;

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
}
