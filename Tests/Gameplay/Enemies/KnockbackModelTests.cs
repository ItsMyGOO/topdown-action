using GodotGameTemplate.Gameplay.Enemies.Feedback;

namespace GodotGameTemplate.Tests.Gameplay.Enemies;

public sealed class KnockbackModelTests
{
    [Fact]
    public void Initially_ZeroVelocity()
    {
        var knockback = new KnockbackModel();

        Assert.Equal(0f, knockback.X);
        Assert.Equal(0f, knockback.Y);
    }

    [Fact]
    public void Apply_WritesDirectionTimesImpulse()
    {
        var knockback = new KnockbackModel();

        knockback.Apply(1f, 0f, 90f);

        Assert.Equal(90f, knockback.X);
        Assert.Equal(0f, knockback.Y);
    }

    [Fact]
    public void Apply_IsOverwrite_NotAdditive()
    {
        var knockback = new KnockbackModel();

        knockback.Apply(1f, 0f, 90f);
        knockback.Apply(1f, 0f, 90f);

        Assert.Equal(90f, knockback.X);
    }

    [Fact]
    public void Tick_DecaysVelocityExponentially()
    {
        var knockback = new KnockbackModel();
        knockback.Apply(1f, 0f, 100f);

        knockback.Tick(0.1f);

        Assert.True(knockback.X < 100f);
        Assert.True(knockback.X > 0f);
    }

    [Fact]
    public void Tick_Repeatedly_VelocityApproachesZero()
    {
        var knockback = new KnockbackModel();
        knockback.Apply(0.6f, 0.8f, 200f);

        for (var i = 0; i < 100; i++)
        {
            knockback.Tick(0.1f);
        }

        Assert.True(knockback.X < 0.01f);
        Assert.True(knockback.Y < 0.01f);
    }

    [Fact]
    public void Tick_NonPositiveDelta_DoesNothing()
    {
        var knockback = new KnockbackModel();
        knockback.Apply(1f, 0f, 90f);

        knockback.Tick(0f);
        knockback.Tick(-1f);

        Assert.Equal(90f, knockback.X);
    }
}
