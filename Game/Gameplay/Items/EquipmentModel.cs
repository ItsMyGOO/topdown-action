using System;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 轻量装备模型（三槽：武器/护甲/饰品）。
/// </summary>
public sealed class EquipmentModel
{
    public ItemInstance? Weapon { get; private set; }

    public ItemInstance? Armor { get; private set; }

    public ItemInstance? Accessory { get; private set; }

    /// <summary>
    /// 穿戴一件装备。首版：直接覆盖对应槽位。
    /// </summary>
    public void Equip(ItemInstance item)
    {
        switch (item.Slot)
        {
            case ItemSlot.Weapon:
                Weapon = item;
                break;
            case ItemSlot.Armor:
                Armor = item;
                break;
            case ItemSlot.Accessory:
                Accessory = item;
                break;
        }
    }

    /// <summary>
    /// 清空全部装备槽。
    /// </summary>
    public void Clear()
    {
        Weapon = null;
        Armor = null;
        Accessory = null;
    }

    /// <summary>
    /// 直接设置整套装备数据。
    /// </summary>
    public void SetEquipment(
        ItemInstance? weapon = null,
        ItemInstance? armor = null,
        ItemInstance? accessory = null
    )
    {
        ValidateSlot(weapon, ItemSlot.Weapon);
        ValidateSlot(armor, ItemSlot.Armor);
        ValidateSlot(accessory, ItemSlot.Accessory);

        Weapon = weapon;
        Armor = armor;
        Accessory = accessory;
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
