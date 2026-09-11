using GodotGameTemplate.Gameplay.Progression;

namespace GodotGameTemplate.Tests.Gameplay.Progression;

public sealed class LevelingModelTests
{
    [Fact]
    public void InitialState_StartsAtLevel1WithZeroXp()
    {
        var leveling = new LevelingModel();

        Assert.Equal(1, leveling.Level);
        Assert.Equal(0, leveling.CurrentXp);
        Assert.Equal(LevelingModel.BaseXpPerLevel, leveling.XpToNextLevel);
    }

    [Fact]
    public void AddXp_WhenBelowThreshold_AccumulatesWithoutLevelUp()
    {
        var leveling = new LevelingModel();

        var result = leveling.AddXp(30);

        Assert.Equal(0, result.LevelsGained);
        Assert.Equal(1, result.NewLevel);
        Assert.Equal(30, leveling.CurrentXp);
        Assert.Equal(1, leveling.Level);
    }

    [Fact]
    public void AddXp_WhenReachesThreshold_LevelsUpAndCarriesOverflow()
    {
        var leveling = new LevelingModel();

        var result = leveling.AddXp(LevelingModel.BaseXpPerLevel + 10);

        Assert.Equal(1, result.LevelsGained);
        Assert.Equal(2, result.NewLevel);
        Assert.Equal(2, leveling.Level);
        Assert.Equal(10, leveling.CurrentXp);
    }

    [Fact]
    public void AddXp_WhenOverflowSpansMultipleLevels_LevelsUpSeveralTimes()
    {
        var leveling = new LevelingModel();

        // 1 -> 2 需要 50，2 -> 3 需要 100；共 160 溢出 10。
        var result = leveling.AddXp(160);

        Assert.Equal(2, result.LevelsGained);
        Assert.Equal(3, result.NewLevel);
        Assert.Equal(3, leveling.Level);
        Assert.Equal(10, leveling.CurrentXp);
    }

    [Fact]
    public void AddXp_WhenZeroOrNegative_IsIgnored()
    {
        var leveling = new LevelingModel();

        Assert.Equal(0, leveling.AddXp(0).LevelsGained);
        Assert.Equal(0, leveling.AddXp(-5).LevelsGained);
        Assert.Equal(1, leveling.Level);
        Assert.Equal(0, leveling.CurrentXp);
    }

    [Fact]
    public void XpToNextLevel_IncreasesWithLevel()
    {
        var leveling = new LevelingModel();
        var first = leveling.XpToNextLevel;

        leveling.AddXp(first);
        var second = leveling.XpToNextLevel;

        Assert.True(second > first);
    }

    [Fact]
    public void ApplyGrowth_AppliesBonusOncePerLevelGain()
    {
        var leveling = new LevelingModel();
        var mana = new ManaModel();
        var stamina = new StaminaModel();

        leveling.AddXp(LevelingModel.BaseXpPerLevel);
        leveling.ApplyGrowth(stamina, mana);

        Assert.Equal(102f, mana.Max);
        Assert.Equal(102f, stamina.Max);
    }

    [Fact]
    public void ApplyGrowth_RepeatedCalls_AreIdempotent()
    {
        var leveling = new LevelingModel();
        var mana = new ManaModel();

        leveling.AddXp(LevelingModel.BaseXpPerLevel);
        leveling.ApplyGrowth(null, mana);
        leveling.ApplyGrowth(null, mana);

        Assert.Equal(102f, mana.Max);
    }

    [Fact]
    public void ApplyGrowth_WhenMultipleLevelsGained_AppliesTotalBonus()
    {
        var leveling = new LevelingModel();
        var mana = new ManaModel();

        leveling.AddXp(160);
        leveling.ApplyGrowth(null, mana);

        Assert.Equal(104f, mana.Max);
    }

    [Fact]
    public void ApplyGrowth_WhenNoLevelGained_DoesNothing()
    {
        var leveling = new LevelingModel();
        var mana = new ManaModel();

        leveling.ApplyGrowth(null, mana);

        Assert.Equal(100f, mana.Max);
    }

    [Fact]
    public void ApplyGrowth_AfterRestore_DoesNotStackBonusOnAlreadyGrownModels()
    {
        var leveling = new LevelingModel();
        var mana = new ManaModel();

        leveling.AddXp(LevelingModel.BaseXpPerLevel);
        leveling.ApplyGrowth(null, mana);
        Assert.Equal(102f, mana.Max);

        // 模拟会话中途读档：模型 Max 仍带着已应用的加成。
        leveling.Restore(2, 0);
        leveling.ApplyGrowth(null, mana);

        Assert.Equal(102f, mana.Max);
    }

    [Fact]
    public void Restore_ResetsGrowthAccountingForReapply()
    {
        var leveling = new LevelingModel();
        leveling.AddXp(LevelingModel.BaseXpPerLevel);

        leveling.Restore(3, 40);

        Assert.Equal(1, leveling.AppliedGrowthLevel);
    }
}
