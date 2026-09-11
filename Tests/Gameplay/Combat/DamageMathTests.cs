using GodotGameTemplate.Gameplay.Combat;

namespace GodotGameTemplate.Tests.Gameplay.Combat;

public sealed class DamageMathTests
{
    [Fact]
    public void Apply_SubtractsFlatResistance()
    {
        Assert.Equal(7, DamageMath.Apply(12, 5));
    }

    [Fact]
    public void Apply_FloorsAtOne()
    {
        Assert.Equal(1, DamageMath.Apply(3, 10));
        Assert.Equal(1, DamageMath.Apply(10, 10));
    }

    [Fact]
    public void Apply_WithZeroOrNegativeResistance_KeepsRawDamage()
    {
        Assert.Equal(8, DamageMath.Apply(8, 0));
        Assert.Equal(9, DamageMath.Apply(9, -4));
    }
}
