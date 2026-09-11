using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class EquipmentModelTests
{
    [Fact]
    public void SetEquipment_ReplacesAllSlots()
    {
        var equipment = new EquipmentModel();
        var weapon = new ItemInstance("weapon_01", ItemSlot.Weapon, ItemRarity.Common, 3);
        var armor = new ItemInstance("armor_01", ItemSlot.Armor, ItemRarity.Magic, 4);
        var accessory = new ItemInstance("accessory_01", ItemSlot.Accessory, ItemRarity.Rare, 5);

        equipment.SetEquipment(weapon, armor, accessory);

        Assert.Equal(weapon, equipment.Weapon);
        Assert.Equal(armor, equipment.Armor);
        Assert.Equal(accessory, equipment.Accessory);
    }

    [Fact]
    public void Clear_RemovesAllSlots()
    {
        var equipment = new EquipmentModel();
        equipment.SetEquipment(
            new ItemInstance("weapon_01", ItemSlot.Weapon, ItemRarity.Common, 3),
            new ItemInstance("armor_01", ItemSlot.Armor, ItemRarity.Magic, 4),
            new ItemInstance("accessory_01", ItemSlot.Accessory, ItemRarity.Rare, 5)
        );

        equipment.Clear();

        Assert.Null(equipment.Weapon);
        Assert.Null(equipment.Armor);
        Assert.Null(equipment.Accessory);
    }

    [Fact]
    public void Equip_StoresItemByItsOwnSlot()
    {
        var equipment = new EquipmentModel();
        var helmet = new ItemInstance("helmet_01", ItemSlot.Helmet, ItemRarity.Common, 2);
        var boots = new ItemInstance("boots_01", ItemSlot.Boots, ItemRarity.Magic, 3);

        equipment.Equip(helmet);
        equipment.Equip(boots);

        Assert.Equal(helmet, equipment.Get(ItemSlot.Helmet));
        Assert.Equal(boots, equipment.Get(ItemSlot.Boots));
        Assert.Equal(boots, equipment.Boots);
    }

    [Fact]
    public void Equip_SecondItemInSameSlot_ReplacesFirst()
    {
        var equipment = new EquipmentModel();
        equipment.Equip(new ItemInstance("helm_a", ItemSlot.Helmet, ItemRarity.Common, 1));

        var replacement = new ItemInstance("helm_b", ItemSlot.Helmet, ItemRarity.Rare, 5);
        equipment.Equip(replacement);

        Assert.Equal(replacement, equipment.Get(ItemSlot.Helmet));
    }

    [Fact]
    public void Equip_WrongSlotItemForSetEquipment_Throws()
    {
        var equipment = new EquipmentModel();
        var ring = new ItemInstance("ring_01", ItemSlot.Ring, ItemRarity.Common, 1);

        Assert.Throws<ArgumentException>(() => equipment.SetEquipment(weapon: ring));
    }

    [Fact]
    public void SetEquipment_SupportsAllEightSlots()
    {
        var equipment = new EquipmentModel();
        ItemInstance?[] items =
        [
            new("weapon_01", ItemSlot.Weapon, ItemRarity.Common, 1),
            new("armor_01", ItemSlot.Armor, ItemRarity.Common, 1),
            new("accessory_01", ItemSlot.Accessory, ItemRarity.Common, 1),
            new("helmet_01", ItemSlot.Helmet, ItemRarity.Common, 1),
            new("gloves_01", ItemSlot.Gloves, ItemRarity.Common, 1),
            new("legs_01", ItemSlot.Legs, ItemRarity.Common, 1),
            new("boots_01", ItemSlot.Boots, ItemRarity.Common, 1),
            new("ring_01", ItemSlot.Ring, ItemRarity.Common, 1),
        ];

        equipment.SetEquipment(
            weapon: items[0],
            armor: items[1],
            accessory: items[2],
            helmet: items[3],
            gloves: items[4],
            legs: items[5],
            boots: items[6],
            ring: items[7]
        );

        foreach (var slot in EquipmentModel.AllSlots)
        {
            Assert.Equal(items[(int)slot], equipment.Get(slot));
        }
    }

    [Fact]
    public void EquippedItems_YieldsOnlyFilledSlots()
    {
        var equipment = new EquipmentModel();
        var weapon = new ItemInstance("weapon_01", ItemSlot.Weapon, ItemRarity.Common, 1);
        equipment.Equip(weapon);

        Assert.Equal([weapon], equipment.EquippedItems);
    }
}
