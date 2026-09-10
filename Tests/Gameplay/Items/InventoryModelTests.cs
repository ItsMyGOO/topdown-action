using GodotGameTemplate.Gameplay.Items;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class InventoryModelTests
{
    [Fact]
    public void TryAdd_FalseWhenFull()
    {
        var inv = new InventoryModel { Capacity = 1 };

        Assert.True(inv.TryAdd(new ItemInstance("w1", ItemSlot.Weapon, ItemRarity.Common, 1)));
        Assert.False(inv.TryAdd(new ItemInstance("w2", ItemSlot.Weapon, ItemRarity.Common, 1)));
    }

    [Fact]
    public void ReplaceItems_ClearsExistingItemsAndUsesProvidedOrder()
    {
        var inv = new InventoryModel { Capacity = 3 };
        var armor = new ItemInstance("armor_01", ItemSlot.Armor, ItemRarity.Common, 1);
        var accessory = new ItemInstance("accessory_01", ItemSlot.Accessory, ItemRarity.Magic, 2);

        inv.TryAdd(new ItemInstance("weapon_01", ItemSlot.Weapon, ItemRarity.Common, 1));

        inv.ReplaceItems([armor, accessory]);

        Assert.Collection(
            inv.Items,
            item => Assert.Equal(armor, item),
            item => Assert.Equal(accessory, item)
        );
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var inv = new InventoryModel();
        inv.TryAdd(new ItemInstance("weapon_01", ItemSlot.Weapon, ItemRarity.Common, 1));
        inv.TryAdd(new ItemInstance("armor_01", ItemSlot.Armor, ItemRarity.Common, 1));

        inv.Clear();

        Assert.Empty(inv.Items);
    }
}
