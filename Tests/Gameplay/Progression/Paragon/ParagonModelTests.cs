using GodotGameTemplate.Gameplay.Progression.Paragon;

namespace GodotGameTemplate.Tests.Gameplay.Progression.Paragon;

public sealed class ParagonModelTests
{
    [Fact]
    public void PointsFromLevel_BelowUnlock_IsZero()
    {
        Assert.Equal(0, ParagonModel.PointsFromLevel(9));
    }

    [Fact]
    public void PointsFromLevel_AtUnlockAndBeyond_IsLinear()
    {
        Assert.Equal(1, ParagonModel.PointsFromLevel(10));
        Assert.Equal(2, ParagonModel.PointsFromLevel(11));
        Assert.Equal(5, ParagonModel.PointsFromLevel(14));
    }

    [Fact]
    public void Allocate_BelowUnlock_ReturnsFalse()
    {
        var paragon = new ParagonModel();

        Assert.False(paragon.Allocate(ParagonCategory.Brutality, level: 9));
        Assert.Equal(0, paragon.TotalSpent);
    }

    [Fact]
    public void Allocate_AtUnlock_SpendsPoint()
    {
        var paragon = new ParagonModel();

        Assert.True(paragon.Allocate(ParagonCategory.Brutality, level: 10));

        Assert.Equal(1, paragon.Ranks[ParagonCategory.Brutality]);
        Assert.Equal(1, paragon.TotalSpent);
    }

    [Fact]
    public void Allocate_WithoutPoints_ReturnsFalse()
    {
        var paragon = new ParagonModel();

        Assert.True(paragon.Allocate(ParagonCategory.Vitality, level: 10));
        Assert.False(paragon.Allocate(ParagonCategory.Vitality, level: 10)); // 只赚了 1 点。
    }

    [Fact]
    public void AvailablePoints_ClampsAtZero()
    {
        var paragon = new ParagonModel();
        paragon.Allocate(ParagonCategory.Brutality, 10);
        paragon.Allocate(ParagonCategory.Brutality, 10);

        // 10 级只有 1 点。
        Assert.Equal(0, paragon.AvailablePoints(10));
    }

    [Fact]
    public void Restore_LoadsRanks()
    {
        var paragon = new ParagonModel();

        paragon.Restore([(ParagonCategory.Cunning, 3), (ParagonCategory.Alacrity, 1)]);

        Assert.Equal(3, paragon.Ranks[ParagonCategory.Cunning]);
        Assert.Equal(1, paragon.Ranks[ParagonCategory.Alacrity]);
    }
}
