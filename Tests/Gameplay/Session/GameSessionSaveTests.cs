using GodotGameTemplate.Gameplay.Items;
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
}
