using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class LootDropperTests
{
    [Fact]
    public void RollBasicDrop_SameSeed_ProducesIdenticalDrops()
    {
        var a = new LootDropper(42);
        var b = new LootDropper(42);

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(a.RollBasicDrop(), b.RollBasicDrop());
        }
    }

    [Fact]
    public void RollBasicDrop_AffixCountMatchesRarityBudget()
    {
        var dropper = new LootDropper(7);

        for (var i = 0; i < 50; i++)
        {
            var drop = dropper.RollBasicDrop();

            var expected = drop.Rarity switch
            {
                ItemRarity.Rare => 2,
                ItemRarity.Magic => 1,
                _ => 0,
            };
            Assert.Equal(expected, drop.Affixes.Length);
        }
    }

    [Fact]
    public void RollBasicDrop_ManyRolls_ReachEverySlot()
    {
        var dropper = new LootDropper(7);
        var seenSlots = new HashSet<ItemSlot>();

        for (var i = 0; i < 400; i++)
        {
            seenSlots.Add(dropper.RollBasicDrop().Slot);
        }

        Assert.Subset(new HashSet<ItemSlot>(EquipmentModel.AllSlots), seenSlots);
    }

    [Fact]
    public void RollBasicDrop_PowerFollowsRarity()
    {
        var dropper = new LootDropper(3);

        for (var i = 0; i < 50; i++)
        {
            var drop = dropper.RollBasicDrop();

            var expected = drop.Rarity switch
            {
                ItemRarity.Rare => 5,
                ItemRarity.Magic => 3,
                _ => 1,
            };
            Assert.Equal(expected, drop.Power);
        }
    }
}
