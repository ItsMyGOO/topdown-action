namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 背包与装备之间的“搬运规则”（纯逻辑，可单测）。
/// </summary>
public static class InventoryEquipmentService
{
    /// <summary>
    /// 尝试把 <paramref name="item"/> 从背包移除并装备到 <paramref name="equipment"/> 上。
    /// </summary>
    /// <returns>
    /// 若物品存在于背包并成功移除，则返回 <c>true</c>，并调用 <see cref="EquipmentModel.Equip"/>；
    /// 否则返回 <c>false</c> 且不产生副作用。
    /// </returns>
    public static bool TryEquip(
        InventoryModel inventory,
        EquipmentModel equipment,
        ItemInstance item
    )
    {
        if (!inventory.Remove(item))
        {
            return false;
        }

        equipment.Equip(item);
        return true;
    }
}
