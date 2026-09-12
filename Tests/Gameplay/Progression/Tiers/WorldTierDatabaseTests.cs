using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Progression.Tiers;

namespace GodotGameTemplate.Tests.Gameplay.Progression.Tiers;

public sealed class WorldTierDatabaseTests
{
    [Fact]
    public void Catalog_HasThreeTiers_InOrder()
    {
        Assert.Equal(3, WorldTierDatabase.Catalog.Count);
        Assert.Equal(1, WorldTierDatabase.Catalog[0].Tier);
        Assert.Equal(2, WorldTierDatabase.Catalog[1].Tier);
        Assert.Equal(3, WorldTierDatabase.Catalog[2].Tier);
    }

    [Fact]
    public void Normal_Tier_IsBaseline()
    {
        var normal = WorldTierDatabase.Get(1);

        Assert.Equal(1f, normal.EnemyHpMultiplier);
        Assert.Equal(1f, normal.EnemyDamageMultiplier);
        Assert.Equal(1f, normal.XpMultiplier);
        Assert.Equal(ItemRarity.Common, normal.DropRarityFloor);
    }

    [Fact]
    public void Nightmare_Tier_ScalesEnemies()
    {
        var nightmare = WorldTierDatabase.Get(2);

        Assert.Equal(2.5f, nightmare.EnemyHpMultiplier);
        Assert.Equal(1.8f, nightmare.EnemyDamageMultiplier);
        Assert.Equal(1.75f, nightmare.XpMultiplier);
        Assert.Equal(ItemRarity.Rare, nightmare.DropRarityFloor);
    }

    [Fact]
    public void Hell_Tier_IsHardest()
    {
        var hell = WorldTierDatabase.Get(3);

        Assert.Equal(5f, hell.EnemyHpMultiplier);
        Assert.Equal(2.6f, hell.EnemyDamageMultiplier);
        Assert.Equal(2.5f, hell.XpMultiplier);
        Assert.Equal(ItemRarity.Rare, hell.DropRarityFloor);
    }

    [Fact]
    public void Get_OutOfRange_ClampsToValidTiers()
    {
        Assert.Equal(1, WorldTierDatabase.Get(0).Tier);
        Assert.Equal(1, WorldTierDatabase.Get(-5).Tier);
        Assert.Equal(3, WorldTierDatabase.Get(99).Tier);
    }
}
