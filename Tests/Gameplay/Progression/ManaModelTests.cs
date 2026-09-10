using GodotGameTemplate.Gameplay.Progression;

namespace GodotGameTemplate.Tests.Gameplay.Progression;

public sealed class ManaModelTests
{
    [Fact]
    public void TryConsume_WhenEnoughMana_ReturnsTrueAndReducesCurrent()
    {
        var mana = new ManaModel();

        var ok = mana.TryConsume(30f);

        Assert.True(ok);
        Assert.Equal(70f, mana.Current);
    }

    [Fact]
    public void TryConsume_WhenManaNotEnough_ReturnsFalseAndKeepsCurrent()
    {
        var mana = new ManaModel();

        var ok = mana.TryConsume(200f);

        Assert.False(ok);
        Assert.Equal(mana.Max, mana.Current);
    }

    [Fact]
    public void Tick_RegenClampsCurrentToMax()
    {
        var mana = new ManaModel();
        mana.TryConsume(50f);

        mana.Tick(10f);

        Assert.Equal(mana.Max, mana.Current);
    }
}
