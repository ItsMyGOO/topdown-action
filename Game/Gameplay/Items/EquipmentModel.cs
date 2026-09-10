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
}
