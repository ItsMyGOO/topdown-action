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

    [Fact]
    public void ApplyGrowth_CombinesExternalEquipmentBonusWithLevelBonus()
    {
        var leveling = new LevelingModel { ExternalManaBonus = 12f, ExternalStaminaBonus = 7f };
        var mana = new ManaModel();
        var stamina = new StaminaModel();

        leveling.AddXp(LevelingModel.BaseXpPerLevel);
        leveling.ApplyGrowth(stamina, mana);

        // 等级加成 +2 与装备加成 12/7 叠加在基准 100 上。
        Assert.Equal(114f, mana.Max);
        Assert.Equal(109f, stamina.Max);
    }

    [Fact]
    public void ApplyGrowth_WithExternalBonus_IsIdempotentAcrossReloads()
    {
        var leveling = new LevelingModel { ExternalManaBonus = 10f };
        var mana = new ManaModel();

        leveling.AddXp(LevelingModel.BaseXpPerLevel);
        leveling.ApplyGrowth(null, mana);
        Assert.Equal(112f, mana.Max);

        leveling.Restore(2, 0);
        leveling.ApplyGrowth(null, mana);

        Assert.Equal(112f, mana.Max);
    }

    [Fact]
    public void DesiredMax_CombinesBaseLevelAndExternalBonuses()
    {
        var leveling = new LevelingModel { ExternalManaBonus = 15f, ExternalStaminaBonus = 4f };

        Assert.Equal(115f, leveling.DesiredManaMax);
        Assert.Equal(104f, leveling.DesiredStaminaMax);

        leveling.AddXp(LevelingModel.BaseXpPerLevel);

        Assert.Equal(117f, leveling.DesiredManaMax);
        Assert.Equal(106f, leveling.DesiredStaminaMax);
    }

    [Fact]
    public void ApplyGrowth_AppliesHealthGrowthAndClampsCurrent()
    {
        var leveling = new LevelingModel();
        var health = new HealthModel { Max = 200f };
        health.Heal(999f); // 当前生命撑到 200。

        leveling.AddXp(LevelingModel.BaseXpPerLevel);
        leveling.ApplyGrowth(null, null, health);

        // 生命上限按等级成长；超出新上限的当前值被钳制。
        Assert.Equal(105f, health.Max);
        Assert.Equal(105f, health.Current);
    }

    [Fact]
    public void ApplyGrowth_HealthBelowNewMax_KeepsCurrent()
    {
        var leveling = new LevelingModel();
        var health = new HealthModel();
        health.TakeDamage(50f);

        leveling.AddXp(LevelingModel.BaseXpPerLevel);
        leveling.ApplyGrowth(null, null, health);

        Assert.Equal(105f, health.Max);
        Assert.Equal(50f, health.Current);
    }
}
