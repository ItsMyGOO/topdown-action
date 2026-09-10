using GodotGameTemplate.Gameplay.Items;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class Inventory3Tests
{
    [Fact]
    public void NewInventory_HasThreeEmptySlots()
    {
        var inv = new Inventory3();
        Assert.Equal(3, inv.Slots.Count);
        Assert.All(inv.Slots, s => Assert.Null(s));
    }

    [Fact]
    public void TryAdd_PutsItemIntoFirstEmptySlot()
    {
        var inv = new Inventory3();

        var ok = inv.TryAdd(new ItemStack("gold", 1));

        Assert.True(ok);
        Assert.NotNull(inv.Slots[0]);
        Assert.Equal("gold", inv.Slots[0]!.Value.ItemId);
        Assert.Equal(1, inv.Slots[0]!.Value.Quantity);
        Assert.Null(inv.Slots[1]);
        Assert.Null(inv.Slots[2]);
    }

    [Fact]
    public void TryAdd_WhenFull_ReturnsFalseAndDoesNotOverwrite()
    {
        var inv = new Inventory3();
        Assert.True(inv.TryAdd(new ItemStack("a", 1)));
        Assert.True(inv.TryAdd(new ItemStack("b", 1)));
        Assert.True(inv.TryAdd(new ItemStack("c", 1)));

        var ok = inv.TryAdd(new ItemStack("d", 1));

        Assert.False(ok);
        Assert.Equal("a", inv.Slots[0]!.Value.ItemId);
        Assert.Equal("b", inv.Slots[1]!.Value.ItemId);
        Assert.Equal("c", inv.Slots[2]!.Value.ItemId);
    }
}
