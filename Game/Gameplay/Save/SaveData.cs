using System.Collections.Generic;
using System.Linq;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Progression;

namespace GodotGameTemplate.Gameplay.Save;

/// <summary>
/// 会话存档 DTO。
/// </summary>
public sealed class SaveData
{
    public int Gold { get; set; }

    /// <summary>
    /// 角色等级；旧存档缺省为 1。
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// 当前等级内已积累经验；旧存档缺省为 0。
    /// </summary>
    public int CurrentXp { get; set; }

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
        EquipmentModel equipment,
        LevelingModel? leveling = null
    )
    {
        return new SaveData
        {
            Gold = gold,
            Level = leveling?.Level ?? 1,
            CurrentXp = leveling?.CurrentXp ?? 0,
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
        EquipmentModel equipment,
        LevelingModel? leveling = null
    )
    {
        inventory.ReplaceItems(data.InventoryItems.Select(item => item.ToItemInstance()));
        equipment.SetEquipment(
            data.EquipmentSlots.Weapon?.ToItemInstance(),
            data.EquipmentSlots.Armor?.ToItemInstance(),
            data.EquipmentSlots.Accessory?.ToItemInstance()
        );

        if (leveling != null)
        {
            leveling.Restore(
                data.Level > 0 ? data.Level : 1,
                data.CurrentXp > 0 ? data.CurrentXp : 0
            );
        }

        return data.Gold;
    }
}
