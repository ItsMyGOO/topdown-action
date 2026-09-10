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
}
