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
}
