using System.Collections.Generic;
using System.Linq;
using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Gameplay.Save;

/// <summary>
/// 会话存档 DTO。
/// </summary>
public sealed class SaveData
{
    public int Gold { get; set; }

    public List<SaveItemInstanceData> InventoryItems { get; set; } = [];

    public SaveEquipmentSlotsData EquipmentSlots { get; set; } = new();
}

/// <summary>
/// 可序列化的物品实例 DTO，保存完整运行时实例字段。
/// </summary>
public sealed class SaveItemInstanceData
{
    public string Id { get; set; } = string.Empty;

    public ItemSlot Slot { get; set; }

    public ItemRarity Rarity { get; set; }

    public int Power { get; set; }

    public static SaveItemInstanceData FromItemInstance(ItemInstance item)
    {
        return new SaveItemInstanceData
        {
            Id = item.Id,
            Slot = item.Slot,
            Rarity = item.Rarity,
            Power = item.Power,
        };
    }

    public ItemInstance ToItemInstance()
    {
        return new ItemInstance(Id, Slot, Rarity, Power);
    }
}

/// <summary>
/// 装备槽位 DTO。
/// </summary>
public sealed class SaveEquipmentSlotsData
{
    public SaveItemInstanceData? Weapon { get; set; }

    public SaveItemInstanceData? Armor { get; set; }

    public SaveItemInstanceData? Accessory { get; set; }
}

/// <summary>
/// 会话状态与存档 DTO 之间的纯逻辑映射器，便于单测。
/// </summary>
public static class SaveDataMapper
{
    public static SaveData FromState(
        int gold,
        IEnumerable<ItemInstance> inventoryItems,
        EquipmentModel equipment
    )
    {
        return new SaveData
        {
            Gold = gold,
            InventoryItems = [.. inventoryItems.Select(SaveItemInstanceData.FromItemInstance)],
            EquipmentSlots = new SaveEquipmentSlotsData
            {
                Weapon =
                    equipment.Weapon == null
                        ? null
                        : SaveItemInstanceData.FromItemInstance(equipment.Weapon),
                Armor =
                    equipment.Armor == null
                        ? null
                        : SaveItemInstanceData.FromItemInstance(equipment.Armor),
                Accessory =
                    equipment.Accessory == null
                        ? null
                        : SaveItemInstanceData.FromItemInstance(equipment.Accessory),
            },
        };
    }

    public static int ApplyToState(
        SaveData data,
        InventoryModel inventory,
        EquipmentModel equipment
    )
    {
        inventory.ReplaceItems(data.InventoryItems.Select(item => item.ToItemInstance()));
        equipment.SetEquipment(
            data.EquipmentSlots.Weapon?.ToItemInstance(),
            data.EquipmentSlots.Armor?.ToItemInstance(),
            data.EquipmentSlots.Accessory?.ToItemInstance()
        );

        return data.Gold;
    }
}
