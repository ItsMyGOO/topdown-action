using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class StashServiceTests
{
    private static readonly ItemInstance Sword =
        new("weapon_01", ItemSlot.Weapon, ItemRarity.Rare, 5);

    [Fact]
    public void Deposit_MovesItemFromInventoryToStash()
    {
        var inventory = new InventoryModel();
        inventory.TryAdd(Sword);
        var stash = new InventoryModel();

        var ok = StashService.TryDeposit(inventory, stash, Sword);

        Assert.True(ok);
        Assert.Empty(inventory.Items);
        Assert.Equal([Sword], stash.Items);
    }

    [Fact]
    public void Deposit_ItemNotInInventory_ReturnsFalseWithNoSideEffects()
    {
        var inventory = new InventoryModel();
        var stash = new InventoryModel();

        Assert.False(StashService.TryDeposit(inventory, stash, Sword));
        Assert.Empty(stash.Items);
    }

    [Fact]
    public void Deposit_StashFull_ReturnsFalseAndKeepsItemInInventory()
    {
        var inventory = new InventoryModel();
        inventory.TryAdd(Sword);
        var stash = new InventoryModel { Capacity = 1 };
        stash.TryAdd(new ItemInstance("other", ItemSlot.Armor, ItemRarity.Common, 1));

        Assert.False(StashService.TryDeposit(inventory, stash, Sword));
        Assert.Equal([Sword], inventory.Items);
    }

    [Fact]
    public void Withdraw_MovesItemFromStashToInventory()
    {
        var stash = new InventoryModel();
        stash.TryAdd(Sword);
        var inventory = new InventoryModel();

        var ok = StashService.TryWithdraw(stash, inventory, Sword);

        Assert.True(ok);
        Assert.Empty(stash.Items);
        Assert.Equal([Sword], inventory.Items);
    }

    [Fact]
    public void Withdraw_InventoryFull_ReturnsFalseAndKeepsItemInStash()
    {
        var stash = new InventoryModel();
        stash.TryAdd(Sword);
        var inventory = new InventoryModel { Capacity = 1 };
        inventory.TryAdd(new ItemInstance("other", ItemSlot.Armor, ItemRarity.Common, 1));

        Assert.False(StashService.TryWithdraw(stash, inventory, Sword));
        Assert.Equal([Sword], stash.Items);
    }
}
