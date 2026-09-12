using System.Collections.Generic;
using System.Linq;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Progression;
using GodotGameTemplate.Gameplay.Progression.Classes;
using GodotGameTemplate.Gameplay.Progression.Talents;
using GodotGameTemplate.Gameplay.Progression.Tiers;

namespace GodotGameTemplate.Gameplay.Save;

/// <summary>
/// 可序列化的天赋等级 DTO。
/// </summary>
public sealed class SaveTalentData
{
    public string Id { get; set; } = string.Empty;

    public int Rank { get; set; }
}

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

    /// <summary>
    /// 天赋等级；旧存档缺省为空（未加点）。
    /// </summary>
    public List<SaveTalentData> Talents { get; set; } = [];

    /// <summary>
    /// 药水当前充能；旧存档缺省为 0，加载时视为满充能（缺省语义而非「用光」）。
    /// </summary>
    public int Potions { get; set; }

    /// <summary>
    /// 当前职业 Id；旧存档缺省为空，加载时回退默认职业。
    /// </summary>
    public string ClassId { get; set; } = string.Empty;

    /// <summary>
    /// 世界等级（1-3）；旧存档缺省为 0，加载时钳制为 1。
    /// </summary>
    public int WorldTier { get; set; }

    public List<SaveItemInstanceData> InventoryItems { get; set; } = [];

    public SaveEquipmentSlotsData EquipmentSlots { get; set; } = new();
}

/// <summary>
/// 可序列化的词缀行 DTO。
/// </summary>
public sealed class SaveAffixLineData
{
    public AffixStat Stat { get; set; }

    public float Value { get; set; }

    public static SaveAffixLineData FromAffixLine(AffixLine affix)
    {
        return new SaveAffixLineData { Stat = affix.Stat, Value = affix.Value };
    }

    public AffixLine ToAffixLine()
    {
        return new AffixLine(Stat, Value);
    }
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

    /// <summary>
    /// 词缀行；旧存档缺省为 null，加载后视为无词条。
    /// </summary>
    public List<SaveAffixLineData>? Affixes { get; set; }

    public static SaveItemInstanceData FromItemInstance(ItemInstance item)
    {
        return new SaveItemInstanceData
        {
            Id = item.Id,
            Slot = item.Slot,
            Rarity = item.Rarity,
            Power = item.Power,
            Affixes =
                item.Affixes.Length == 0
                    ? null
                    : [.. item.Affixes.Select(SaveAffixLineData.FromAffixLine)],
        };
    }

    public ItemInstance ToItemInstance()
    {
        var affixes = Affixes?.Select(line => line.ToAffixLine()).ToArray();
        return new ItemInstance(Id, Slot, Rarity, Power, affixes);
    }
}

/// <summary>
/// 装备槽位 DTO（八槽；旧存档缺省字段为 null → 未穿戴）。
/// </summary>
public sealed class SaveEquipmentSlotsData
{
    public SaveItemInstanceData? Weapon { get; set; }

    public SaveItemInstanceData? Armor { get; set; }

    public SaveItemInstanceData? Accessory { get; set; }

    public SaveItemInstanceData? Helmet { get; set; }

    public SaveItemInstanceData? Gloves { get; set; }

    public SaveItemInstanceData? Legs { get; set; }

    public SaveItemInstanceData? Boots { get; set; }

    public SaveItemInstanceData? Ring { get; set; }
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
        LevelingModel? leveling = null,
        TalentModel? talents = null,
        PotionChargesModel? potions = null,
        string? classId = null,
        int worldTier = 0
    )
    {
        return new SaveData
        {
            Gold = gold,
            Level = leveling?.Level ?? 1,
            CurrentXp = leveling?.CurrentXp ?? 0,
            Potions = potions?.Available ?? 0,
            ClassId = classId ?? string.Empty,
            WorldTier = WorldTierDatabase.Get(worldTier).Tier,
            Talents =
                talents == null
                    ? []
                    :
                    [
                        .. talents.Ranks.Select(pair => new SaveTalentData
                        {
                            Id = pair.Key,
                            Rank = pair.Value,
                        }),
                    ],
            InventoryItems = [.. inventoryItems.Select(SaveItemInstanceData.FromItemInstance)],
            EquipmentSlots = SaveEquipmentSlotsFrom(equipment),
        };
    }

    private static SaveEquipmentSlotsData SaveEquipmentSlotsFrom(EquipmentModel equipment)
    {
        SaveItemInstanceData? Get(ItemSlot slot)
        {
            var item = equipment.Get(slot);

            return item == null ? null : SaveItemInstanceData.FromItemInstance(item);
        }

        return new SaveEquipmentSlotsData
        {
            Weapon = Get(ItemSlot.Weapon),
            Armor = Get(ItemSlot.Armor),
            Accessory = Get(ItemSlot.Accessory),
            Helmet = Get(ItemSlot.Helmet),
            Gloves = Get(ItemSlot.Gloves),
            Legs = Get(ItemSlot.Legs),
            Boots = Get(ItemSlot.Boots),
            Ring = Get(ItemSlot.Ring),
        };
    }

    public static int ApplyToState(
        SaveData data,
        InventoryModel inventory,
        EquipmentModel equipment,
        out string classId,
        out int worldTier,
        LevelingModel? leveling = null,
        TalentModel? talents = null,
        PotionChargesModel? potions = null
    )
    {
        classId = string.IsNullOrEmpty(data.ClassId) ? ClassDatabase.DefaultClassId : data.ClassId;
        worldTier = WorldTierDatabase.Get(data.WorldTier).Tier;

        inventory.ReplaceItems(data.InventoryItems.Select(item => item.ToItemInstance()));

        var slots = data.EquipmentSlots;
        equipment.SetEquipment(
            slots.Weapon?.ToItemInstance(),
            slots.Armor?.ToItemInstance(),
            slots.Accessory?.ToItemInstance(),
            slots.Helmet?.ToItemInstance(),
            slots.Gloves?.ToItemInstance(),
            slots.Legs?.ToItemInstance(),
            slots.Boots?.ToItemInstance(),
            slots.Ring?.ToItemInstance()
        );

        if (leveling != null)
        {
            leveling.Restore(
                data.Level > 0 ? data.Level : 1,
                data.CurrentXp > 0 ? data.CurrentXp : 0
            );
        }

        if (talents != null)
        {
            talents.Restore(data.Talents.Select(talent => (talent.Id, talent.Rank)));
        }

        if (potions != null)
        {
            // 旧档 Potions 缺省为 0：视为「未记录」补满，而不是真的没药。
            potions.Restore(data.Potions > 0 ? data.Potions : potions.MaxCharges);
        }

        return data.Gold;
    }
}
