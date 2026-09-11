using GodotGameTemplate.Gameplay.Enemies;
using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Tests.Gameplay.Enemies;

public sealed class EliteRollerTests
{
    [Fact]
    public void TryRoll_WithZeroChance_ReturnsNull()
    {
        Assert.Null(EliteRoller.TryRoll(0d, new Random(1)));
    }

    [Fact]
    public void TryRoll_WithCertainChance_ReturnsPlanWithSingleAffix()
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var plan = EliteRoller.TryRoll(1d, new Random(seed));

            Assert.NotNull(plan);
            Assert.False(plan!.IsBoss);
            Assert.NotEqual(EliteAffix.None, plan.Affix);
        }
    }

    [Fact]
    public void TryRoll_SturdyAffix_TriplesHpAndDoublesXp()
    {
        var plan = RollUntilAffix(EliteAffix.Sturdy);

        Assert.Equal(3f, plan.HpMultiplier);
        Assert.Equal(2f, plan.XpMultiplier);
        Assert.Equal(2f, plan.DamageMultiplier);
        Assert.Equal(0, plan.GoldBonus);
        Assert.Equal(1, plan.DropCount);
        Assert.Equal(ItemRarity.Common, plan.RarityFloor);
    }

    [Fact]
    public void TryRoll_CunningAffix_DoublesXpAndUpgradesDrops()
    {
        var plan = RollUntilAffix(EliteAffix.Cunning);

        Assert.Equal(2f, plan.XpMultiplier);
        Assert.Equal(1f, plan.DamageMultiplier);
        Assert.Equal(2, plan.DropCount);
        Assert.Equal(ItemRarity.Magic, plan.RarityFloor);
    }

    [Fact]
    public void TryRoll_RichAffix_GrantsGoldWithinRange()
    {
        for (var seed = 0; seed < 100; seed++)
        {
            var plan = EliteRoller.TryRoll(1d, new Random(seed));

            if (plan!.Affix == EliteAffix.Rich)
            {
                Assert.InRange(plan.GoldBonus, 10, 25);
                return;
            }
        }

        Assert.Fail("Rich affix never rolled in 100 tries.");
    }

    [Fact]
    public void Boss_ReturnsFixedReinforcedPlan()
    {
        var plan = EliteRoller.Boss();

        Assert.True(plan.IsBoss);
        Assert.Equal(10f, plan.HpMultiplier);
        Assert.Equal(10f, plan.XpMultiplier);
        Assert.Equal(3f, plan.DamageMultiplier);
        Assert.Equal(50, plan.GoldBonus);
        Assert.Equal(2, plan.DropCount);
        Assert.Equal(ItemRarity.Rare, plan.RarityFloor);
    }

    [Fact]
    public void TryRoll_SameSeed_ProducesSamePlan()
    {
        var a = EliteRoller.TryRoll(1d, new Random(77));
        var b = EliteRoller.TryRoll(1d, new Random(77));

        Assert.Equal(a, b);
    }

    private static ElitePlan RollUntilAffix(EliteAffix affix)
    {
        for (var seed = 0; seed < 200; seed++)
        {
            var plan = EliteRoller.TryRoll(1d, new Random(seed));

            if (plan!.Affix == affix)
            {
                return plan;
            }
        }

        Assert.Fail($"{affix} never rolled in 200 tries.");
        return EliteRoller.Boss();
    }
}
