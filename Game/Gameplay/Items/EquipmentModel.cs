using System;
using System.Collections.Generic;
using System.Linq;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 装备模型（暗黑式八槽：武器/胸甲/护符/头/手/腿/鞋/戒指）。
/// </summary>
public sealed class EquipmentModel
{
    /// <summary>
    /// 全部装备槽位（定义顺序即展示顺序）。
    /// </summary>
    public static IReadOnlyList<ItemSlot> AllSlots { get; } =
    [
        ItemSlot.Weapon,
        ItemSlot.Armor,
        ItemSlot.Accessory,
        ItemSlot.Helmet,
        ItemSlot.Gloves,
        ItemSlot.Legs,
        ItemSlot.Boots,
        ItemSlot.Ring,
    ];

    private readonly Dictionary<ItemSlot, ItemInstance?> _slots = [];

    public ItemInstance? Weapon => Get(ItemSlot.Weapon);

    public ItemInstance? Armor => Get(ItemSlot.Armor);

    public ItemInstance? Accessory => Get(ItemSlot.Accessory);

    public ItemInstance? Helmet => Get(ItemSlot.Helmet);

    public ItemInstance? Gloves => Get(ItemSlot.Gloves);

    public ItemInstance? Legs => Get(ItemSlot.Legs);

    public ItemInstance? Boots => Get(ItemSlot.Boots);

    public ItemInstance? Ring => Get(ItemSlot.Ring);

    /// <summary>
    /// 已装备的物品（跳过空槽）。
    /// </summary>
    public IEnumerable<ItemInstance> EquippedItems => AllSlots.Select(Get).OfType<ItemInstance>();

    /// <summary>
    /// 穿戴一件装备：按物品自身槽位覆盖该槽。
    /// </summary>
    public void Equip(ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _slots[item.Slot] = item;
    }

    /// <summary>
    /// 读取一个槽位的物品（未穿戴为 null）。
    /// </summary>
    public ItemInstance? Get(ItemSlot slot)
    {
        return _slots.TryGetValue(slot, out var item) ? item : null;
    }

    /// <summary>
    /// 清空全部装备槽。
    /// </summary>
    public void Clear()
    {
        foreach (var slot in AllSlots)
        {
            _slots[slot] = null;
        }
    }

    /// <summary>
    /// 直接设置整套装备数据（可选命名参数，跳过 null 槽位；不校验会先整体抛出）。
    /// </summary>
    public void SetEquipment(
        ItemInstance? weapon = null,
        ItemInstance? armor = null,
        ItemInstance? accessory = null,
        ItemInstance? helmet = null,
        ItemInstance? gloves = null,
        ItemInstance? legs = null,
        ItemInstance? boots = null,
        ItemInstance? ring = null
    )
    {
        ValidateSlot(weapon, ItemSlot.Weapon);
        ValidateSlot(armor, ItemSlot.Armor);
        ValidateSlot(accessory, ItemSlot.Accessory);
        ValidateSlot(helmet, ItemSlot.Helmet);
        ValidateSlot(gloves, ItemSlot.Gloves);
        ValidateSlot(legs, ItemSlot.Legs);
        ValidateSlot(boots, ItemSlot.Boots);
        ValidateSlot(ring, ItemSlot.Ring);

        Clear();
        TryStore(weapon);
        TryStore(armor);
        TryStore(accessory);
        TryStore(helmet);
        TryStore(gloves);
        TryStore(legs);
        TryStore(boots);
        TryStore(ring);
    }

    private void TryStore(ItemInstance? item)
    {
        if (item != null)
        {
            _slots[item.Slot] = item;
        }
    }

    private static void ValidateSlot(ItemInstance? item, ItemSlot expectedSlot)
    {
        if (item != null && item.Slot != expectedSlot)
        {
            throw new ArgumentException(
                $"Expected {expectedSlot} item, but received {item.Slot}.",
                nameof(item)
            );
        }
    }
}
