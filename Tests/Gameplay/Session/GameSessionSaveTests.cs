using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Progression;
using GodotGameTemplate.Gameplay.Progression.Talents;
using GodotGameTemplate.Gameplay.Save;

namespace GodotGameTemplate.Tests.Gameplay.Session;

public sealed class GameSessionSaveTests
{
    [Fact]
    public void SaveDataMapper_FromState_MapsGoldInventoryAndEquipmentIntoSaveData()
    {
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();
        var sword = new ItemInstance("weapon_01", ItemSlot.Weapon, ItemRarity.Rare, 12);
        var armor = new ItemInstance("armor_01", ItemSlot.Armor, ItemRarity.Magic, 7);
        var charm = new ItemInstance("trinket_01", ItemSlot.Accessory, ItemRarity.Common, 3);

        inventory.ReplaceItems([sword, armor]);
        equipment.SetEquipment(sword, armor, charm);

        var data = SaveDataMapper.FromState(233, inventory.Items, equipment);

        Assert.Equal(233, data.Gold);
        Assert.Collection(
            data.InventoryItems,
            item =>
            {
                Assert.Equal("weapon_01", item.Id);
                Assert.Equal(ItemSlot.Weapon, item.Slot);
                Assert.Equal(ItemRarity.Rare, item.Rarity);
                Assert.Equal(12, item.Power);
            },
            item =>
            {
                Assert.Equal("armor_01", item.Id);
                Assert.Equal(ItemSlot.Armor, item.Slot);
                Assert.Equal(ItemRarity.Magic, item.Rarity);
                Assert.Equal(7, item.Power);
            }
        );
        Assert.Equal("weapon_01", data.EquipmentSlots.Weapon!.Id);
        Assert.Equal("armor_01", data.EquipmentSlots.Armor!.Id);
        Assert.Equal("trinket_01", data.EquipmentSlots.Accessory!.Id);
    }

    [Fact]
    public void SaveDataMapper_ApplyToState_AppliesGoldInventoryAndEquipment()
    {
        var data = new SaveData
        {
            Gold = 99,
            InventoryItems =
            [
                new SaveItemInstanceData
                {
                    Id = "staff_01",
                    Slot = ItemSlot.Weapon,
                    Rarity = ItemRarity.Magic,
                    Power = 8,
                },
                new SaveItemInstanceData
                {
                    Id = "ring_01",
                    Slot = ItemSlot.Accessory,
                    Rarity = ItemRarity.Rare,
                    Power = 5,
                },
            ],
            EquipmentSlots = new SaveEquipmentSlotsData
            {
                Weapon = new SaveItemInstanceData
                {
                    Id = "staff_02",
                    Slot = ItemSlot.Weapon,
                    Rarity = ItemRarity.Rare,
                    Power = 11,
                },
                Accessory = new SaveItemInstanceData
                {
                    Id = "amulet_01",
                    Slot = ItemSlot.Accessory,
                    Rarity = ItemRarity.Common,
                    Power = 2,
                },
            },
        };
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();

        inventory.TryAdd(new ItemInstance("old", ItemSlot.Armor, ItemRarity.Common, 1));
        equipment.Equip(new ItemInstance("old_weapon", ItemSlot.Weapon, ItemRarity.Common, 1));
        var gold = SaveDataMapper.ApplyToState(data, inventory, equipment);

        Assert.Equal(99, gold);
        Assert.Collection(
            inventory.Items,
            item =>
                Assert.Equal(
                    new ItemInstance("staff_01", ItemSlot.Weapon, ItemRarity.Magic, 8),
                    item
                ),
            item =>
                Assert.Equal(
                    new ItemInstance("ring_01", ItemSlot.Accessory, ItemRarity.Rare, 5),
                    item
                )
        );
        Assert.Equal(
            new ItemInstance("staff_02", ItemSlot.Weapon, ItemRarity.Rare, 11),
            equipment.Weapon
        );
        Assert.Null(equipment.Armor);
        Assert.Equal(
            new ItemInstance("amulet_01", ItemSlot.Accessory, ItemRarity.Common, 2),
            equipment.Accessory
        );
    }

    [Fact]
    public void SaveDataMapper_ApplyToState_WithEmptySave_ClearsState()
    {
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();
        var currentItem = new ItemInstance("kept", ItemSlot.Weapon, ItemRarity.Common, 4);

        inventory.TryAdd(currentItem);
        equipment.Equip(currentItem);

        var gold = SaveDataMapper.ApplyToState(new SaveData(), inventory, equipment);

        Assert.Equal(0, gold);
        Assert.Empty(inventory.Items);
        Assert.Null(equipment.Weapon);
    }

    [Fact]
    public void SaveDataMapper_FromState_MapsLevelingIntoSaveData()
    {
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();
        var leveling = new LevelingModel();
        leveling.AddXp(LevelingModel.BaseXpPerLevel + 10);

        var data = SaveDataMapper.FromState(0, inventory.Items, equipment, leveling);

        Assert.Equal(2, data.Level);
        Assert.Equal(10, data.CurrentXp);
    }

    [Fact]
    public void SaveDataMapper_ApplyToState_RestoresLevelingState()
    {
        var data = new SaveData { Level = 3, CurrentXp = 40 };
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();
        var leveling = new LevelingModel();

        SaveDataMapper.ApplyToState(data, inventory, equipment, leveling);

        Assert.Equal(3, leveling.Level);
        Assert.Equal(40, leveling.CurrentXp);
    }

    [Fact]
    public void SaveDataMapper_ApplyToState_WithLegacySaveWithoutLevel_KeepsDefaults()
    {
        var data = new SaveData();
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();
        var leveling = new LevelingModel();
        leveling.AddXp(20);

        SaveDataMapper.ApplyToState(data, inventory, equipment, leveling);

        // 旧存档没有等级字段（反序列化后为默认值 1/0），加载后不应保留内存中的进度。
        Assert.Equal(1, leveling.Level);
        Assert.Equal(0, leveling.CurrentXp);
    }

    [Fact]
    public void SaveDataMapper_ApplyToState_WithInvalidLevelData_FallsBackToDefaults()
    {
        var data = new SaveData { Level = -2, CurrentXp = -10 };
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();
        var leveling = new LevelingModel();

        SaveDataMapper.ApplyToState(data, inventory, equipment, leveling);

        Assert.Equal(1, leveling.Level);
        Assert.Equal(0, leveling.CurrentXp);
    }

    [Fact]
    public void SaveDataMapper_AffixesAndExpandedSlots_RoundTrip()
    {
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();
        var leveling = new LevelingModel();
        leveling.AddXp(LevelingModel.BaseXpPerLevel);

        var helmet = new ItemInstance(
            "helmet_01",
            ItemSlot.Helmet,
            ItemRarity.Rare,
            4,
            [
                new AffixLine(AffixStat.BonusMaxMana, 12f),
                new AffixLine(AffixStat.BonusXpPercent, 15f),
            ]
        );
        equipment.Equip(helmet);
        equipment.Equip(
            new ItemInstance(
                "boots_01",
                ItemSlot.Boots,
                ItemRarity.Magic,
                3,
                [new AffixLine(AffixStat.BonusArmor, 9f)]
            )
        );

        var data = SaveDataMapper.FromState(10, inventory.Items, equipment, leveling);
        var freshEquipment = new EquipmentModel();
        var freshInventory = new InventoryModel();
        var freshLeveling = new LevelingModel();

        SaveDataMapper.ApplyToState(data, freshInventory, freshEquipment, freshLeveling);

        Assert.Equal(helmet, freshEquipment.Get(ItemSlot.Helmet));
        Assert.Equal(
            new ItemInstance(
                "boots_01",
                ItemSlot.Boots,
                ItemRarity.Magic,
                3,
                [new AffixLine(AffixStat.BonusArmor, 9f)]
            ),
            freshEquipment.Get(ItemSlot.Boots)
        );
        Assert.Equal(2, freshLeveling.Level);
    }

    [Fact]
    public void SaveDataMapper_LegacySaveWithoutAffixes_YieldsAffixFreeItems()
    {
        var data = new SaveData();
        data.EquipmentSlots.Weapon = new SaveItemInstanceData
        {
            Id = "legacy_sword",
            Slot = ItemSlot.Weapon,
            Rarity = ItemRarity.Magic,
            Power = 8,
        };
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();

        SaveDataMapper.ApplyToState(data, inventory, equipment);

        var weapon = equipment.Weapon;
        Assert.NotNull(weapon);
        Assert.Empty(weapon!.Affixes);
    }

    [Fact]
    public void SaveDataMapper_TalentRanks_RoundTrip()
    {
        var talents = new TalentModel();
        talents.Allocate(TalentDatabase.Might, 5);
        talents.Allocate(TalentDatabase.Might, 5);
        talents.Allocate(TalentDatabase.Meditation, 5);
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();
        var leveling = new LevelingModel();

        var data = SaveDataMapper.FromState(0, inventory.Items, equipment, leveling, talents);
        var freshTalents = new TalentModel();

        SaveDataMapper.ApplyToState(
            data,
            new InventoryModel(),
            new EquipmentModel(),
            null,
            freshTalents
        );

        Assert.Equal(2, freshTalents.Ranks[TalentDatabase.Might]);
        Assert.Equal(1, freshTalents.Ranks[TalentDatabase.Meditation]);
    }

    [Fact]
    public void SaveDataMapper_LegacySaveWithoutTalents_YieldsEmptyTalents()
    {
        var data = new SaveData();
        var talents = new TalentModel();
        talents.Allocate(TalentDatabase.Might, 5);

        SaveDataMapper.ApplyToState(
            data,
            new InventoryModel(),
            new EquipmentModel(),
            null,
            talents
        );

        Assert.Empty(talents.Ranks);
    }
}
