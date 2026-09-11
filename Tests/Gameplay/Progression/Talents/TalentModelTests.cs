using GodotGameTemplate.Gameplay.Progression.Talents;

namespace GodotGameTemplate.Tests.Gameplay.Progression.Talents;

public sealed class TalentModelTests
{
    [Fact]
    public void InitialState_NoRanks_NoPointsSpent()
    {
        var talents = new TalentModel();

        Assert.Empty(talents.Ranks);
        Assert.Equal(0, talents.PointsSpent);
        Assert.Equal(0, talents.AvailablePoints(1));
    }

    [Fact]
    public void Allocate_WithAvailablePoint_IncreasesRank()
    {
        var talents = new TalentModel();

        Assert.True(talents.Allocate(TalentDatabase.Might, level: 2));

        Assert.Equal(1, talents.Ranks[TalentDatabase.Might]);
        Assert.Equal(1, talents.PointsSpent);
        Assert.Equal(0, talents.AvailablePoints(2));
    }

    [Fact]
    public void Allocate_AtLevelOne_WithoutSpent_ReturnsFalse()
    {
        var talents = new TalentModel();

        Assert.False(talents.Allocate(TalentDatabase.Might, level: 1));
    }

    [Fact]
    public void Allocate_WhenRankCapped_ReturnsFalse()
    {
        var talents = new TalentModel();

        Assert.True(talents.Allocate(TalentDatabase.Might, 5));
        Assert.True(talents.Allocate(TalentDatabase.Might, 5));
        Assert.True(talents.Allocate(TalentDatabase.Might, 5));
        Assert.False(talents.Allocate(TalentDatabase.Might, 5)); // 已满 3 级
    }

    [Fact]
    public void Allocate_PrerequisiteUnmet_ReturnsFalse()
    {
        var talents = new TalentModel();

        Assert.False(talents.Allocate(TalentDatabase.Toughness, 4));

        Assert.True(talents.Allocate(TalentDatabase.Might, 4));
        Assert.True(talents.Allocate(TalentDatabase.Toughness, 4));
        Assert.Equal(1, talents.Ranks[TalentDatabase.Toughness]);
    }

    [Fact]
    public void Allocate_UnknownTalent_ReturnsFalse()
    {
        var talents = new TalentModel();

        Assert.False(talents.Allocate("nope", 5));
    }

    [Fact]
    public void AvailablePoints_ClampsAtZero()
    {
        var talents = new TalentModel();
        talents.Allocate(TalentDatabase.Might, 2);
        talents.Allocate(TalentDatabase.Might, 2);

        // 2 级只有 1 点，已花 2 点 → 可用钳为 0。
        Assert.Equal(0, talents.AvailablePoints(2));
    }

    [Fact]
    public void Restore_ClampsToMaxRank_AndIgnoresUnknownEntries()
    {
        var talents = new TalentModel();

        talents.Restore([(TalentDatabase.Might, 9), ("nope", 3), (TalentDatabase.Endurance, 0)]);

        Assert.Equal(
            TalentDatabase.Get(TalentDatabase.Might)!.MaxRank,
            talents.Ranks[TalentDatabase.Might]
        );
        Assert.False(talents.Ranks.ContainsKey("nope"));
        Assert.False(talents.Ranks.ContainsKey(TalentDatabase.Endurance));
    }
}
