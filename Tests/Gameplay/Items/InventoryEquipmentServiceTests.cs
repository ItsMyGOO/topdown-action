using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class InventoryEquipmentServiceTests
{
    [Fact]
    public void TryEquip_WhenItemExistsInInventory_RemovesAndEquips()
    {
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();
        var item = new ItemInstance("w1", ItemSlot.Weapon, ItemRarity.Common, 1);

        inventory.TryAdd(item);

        var ok = InventoryEquipmentService.TryEquip(inventory, equipment, item);

        Assert.True(ok);
        Assert.DoesNotContain(item, inventory.Items);
        Assert.Equal(item, equipment.Weapon);
    }

    [Fact]
    public void TryEquip_WhenItemNotInInventory_ReturnsFalseAndDoesNothing()
    {
        var inventory = new InventoryModel();
        var equipment = new EquipmentModel();
        var item = new ItemInstance("w1", ItemSlot.Weapon, ItemRarity.Common, 1);

        var ok = InventoryEquipmentService.TryEquip(inventory, equipment, item);

        Assert.False(ok);
        Assert.Null(equipment.Weapon);
        Assert.Empty(inventory.Items);
    }
}
