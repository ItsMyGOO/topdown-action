using GodotGameTemplate.Gameplay.Progression;

namespace GodotGameTemplate.Tests.Gameplay.Progression;

public sealed class HealthModelTests
{
    [Fact]
    public void InitialState_StartsFull()
    {
        var health = new HealthModel();

        Assert.Equal(100f, health.Max);
        Assert.Equal(100f, health.Current);
        Assert.False(health.IsEmpty);
    }

    [Fact]
    public void TakeDamage_ReducesCurrent()
    {
        var health = new HealthModel();

        health.TakeDamage(30f);

        Assert.Equal(70f, health.Current);
    }

    [Fact]
    public void TakeDamage_ClampsAtZeroAndReportsEmpty()
    {
        var health = new HealthModel();

        health.TakeDamage(999f);

        Assert.Equal(0f, health.Current);
        Assert.True(health.IsEmpty);
    }

    [Fact]
    public void TakeDamage_ZeroOrNegative_IsIgnored()
    {
        var health = new HealthModel();

        health.TakeDamage(-5f);
        health.TakeDamage(0f);

        Assert.Equal(100f, health.Current);
    }

    [Fact]
    public void Heal_ClampsAtMax()
    {
        var health = new HealthModel();
        health.TakeDamage(50f);

        health.Heal(30f);
        Assert.Equal(80f, health.Current);

        health.Heal(999f);
        Assert.Equal(100f, health.Current);
    }

    [Fact]
    public void Tick_RegeneratesAndClampsToMax()
    {
        var health = new HealthModel();
        health.TakeDamage(50f);

        health.Tick(1f);

        Assert.Equal(53f, health.Current);

        health.Tick(999f);

        Assert.Equal(100f, health.Current);
    }

    [Fact]
    public void ClampToMax_DropsCurrentWhenMaxShrinks()
    {
        var health = new HealthModel();
        health.Max = 50f;

        health.ClampToMax();

        Assert.Equal(50f, health.Current);
    }

    [Fact]
    public void ResetFull_RestoresToMax()
    {
        var health = new HealthModel();
        health.TakeDamage(99f);

        health.ResetFull();

        Assert.Equal(health.Max, health.Current);
    }
}
