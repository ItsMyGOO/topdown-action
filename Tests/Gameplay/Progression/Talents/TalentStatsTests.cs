using GodotGameTemplate.Gameplay.Progression.Talents;

namespace GodotGameTemplate.Tests.Gameplay.Progression.Talents;

public sealed class TalentStatsTests
{
    [Fact]
    public void Aggregate_EmptyModel_AllFlatZero_MultipliersOne()
    {
        var summary = TalentStats.Aggregate(new TalentModel());

        Assert.Equal(0, summary.BonusDamage);
        Assert.Equal(0f, summary.BonusMaxHealth);
        Assert.Equal(0f, summary.BonusMaxMana);
        Assert.Equal(0f, summary.EvadeRechargeSecondsReduction);
        Assert.Equal(1f, summary.XpMultiplier);
        Assert.Equal(1f, summary.MoveSpeedMultiplier);
    }

    [Fact]
    public void Aggregate_SumsRanksAcrossAllTalents()
    {
        var talents = new TalentModel();
        talents.Restore([
            (TalentDatabase.Might, 2),
            (TalentDatabase.Toughness, 1),
            (TalentDatabase.Meditation, 2),
            (TalentDatabase.Fleetfooted, 1),
            (TalentDatabase.Wisdom, 2),
            (TalentDatabase.Swiftness, 1),
        ]);

        var summary = TalentStats.Aggregate(talents);

        Assert.Equal(2, summary.BonusDamage);
        Assert.Equal(10f, summary.BonusMaxHealth);
        Assert.Equal(10f, summary.BonusMaxMana);
        Assert.Equal(1f, summary.EvadeRechargeSecondsReduction);
        Assert.Equal(1.1f, summary.XpMultiplier);
        Assert.Equal(1.08f, summary.MoveSpeedMultiplier);
    }
}
