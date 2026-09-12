using GodotGameTemplate.Gameplay.Progression.Paragon;

namespace GodotGameTemplate.Tests.Gameplay.Progression.Paragon;

public sealed class ParagonStatsTests
{
    [Fact]
    public void Aggregate_EmptyModel_AllNeutral()
    {
        var summary = ParagonStats.Aggregate(new ParagonModel());

        Assert.Equal(1f, summary.DamageMultiplier);
        Assert.Equal(0f, summary.BonusMaxHealth);
        Assert.Equal(1f, summary.XpMultiplier);
        Assert.Equal(1f, summary.MoveSpeedMultiplier);
    }

    [Fact]
    public void Aggregate_AppliesEachCategoryPerRank()
    {
        var paragon = new ParagonModel();
        paragon.Restore(
        [
            (ParagonCategory.Brutality, 5),
            (ParagonCategory.Vitality, 4),
            (ParagonCategory.Cunning, 10),
            (ParagonCategory.Alacrity, 2),
        ]);

        var summary = ParagonStats.Aggregate(paragon);

        Assert.Equal(1.05f, summary.DamageMultiplier);
        Assert.Equal(12f, summary.BonusMaxHealth);
        Assert.Equal(1.1f, summary.XpMultiplier);
        Assert.Equal(1.01f, summary.MoveSpeedMultiplier);
    }
}
