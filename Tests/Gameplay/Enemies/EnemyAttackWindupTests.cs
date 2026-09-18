using GodotGameTemplate.Gameplay.Enemies.Feedback;

namespace GodotGameTemplate.Tests.Gameplay.Enemies;

public sealed class EnemyAttackWindupTests
{
    [Fact]
    public void Initially_CanBegin_NotWinding()
    {
        var windup = new EnemyAttackWindup();

        Assert.True(windup.CanBegin);
        Assert.False(windup.IsWinding);
    }

    [Fact]
    public void Begin_StartsWinding_BlocksReBegin()
    {
        var windup = new EnemyAttackWindup();

        windup.Begin();

        Assert.True(windup.IsWinding);
        Assert.False(windup.CanBegin);
    }

    [Fact]
    public void Tick_BeforeWindupElapsed_DoesNotResolve()
    {
        var windup = new EnemyAttackWindup(windupSeconds: 0.6);
        windup.Begin();

        Assert.False(windup.Tick(0.3d));
    }

    [Fact]
    public void Tick_AtWindupEnd_ResolveMoment()
    {
        var windup = new EnemyAttackWindup(windupSeconds: 0.6);
        windup.Begin();

        Assert.False(windup.Tick(0.5d));
        Assert.True(windup.Tick(0.2d));
    }

    [Fact]
    public void AfterResolve_CooldownBlocksNewBegin()
    {
        var windup = new EnemyAttackWindup(windupSeconds: 0.6, cooldownSeconds: 0.8);
        windup.Begin();
        windup.Tick(0.6d);

        Assert.False(windup.CanBegin);

        windup.Begin(); // 冷却中被拒。
        Assert.False(windup.IsWinding);

        windup.Tick(0.9d); // 冷却走完。

        Assert.True(windup.CanBegin);
    }

    [Fact]
    public void Cancel_StopsWinding_WithoutCooldown()
    {
        var windup = new EnemyAttackWindup();
        windup.Begin();

        windup.Cancel();

        Assert.False(windup.IsWinding);
        Assert.True(windup.CanBegin);
    }

    [Fact]
    public void Tick_NonPositiveDelta_Ignored()
    {
        var windup = new EnemyAttackWindup(windupSeconds: 0.6);
        windup.Begin();

        Assert.False(windup.Tick(0d));
        Assert.False(windup.Tick(-1d));
        Assert.True(windup.IsWinding);
    }
}
