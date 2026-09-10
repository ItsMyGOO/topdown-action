using GodotGameTemplate.Gameplay.Progression;

namespace GodotGameTemplate.Tests.Gameplay.Progression;

public sealed class StaminaModelTests
{
    [Fact]
    public void TryConsume_WhenEnoughStamina_ReturnsTrueAndReducesCurrent()
    {
        var stamina = new StaminaModel();

        var result = stamina.TryConsume(25f);

        Assert.True(result);
        Assert.Equal(75f, stamina.Current);
    }

    [Fact]
    public void TryConsume_WhenStaminaNotEnough_ReturnsFalseAndKeepsCurrent()
    {
        var stamina = new StaminaModel();
        var firstConsume = stamina.TryConsume(90f);

        var secondConsume = stamina.TryConsume(20f);

        Assert.True(firstConsume);
        Assert.False(secondConsume);
        Assert.Equal(10f, stamina.Current);
    }

    [Fact]
    public void Tick_RegenClampsCurrentToMax()
    {
        var stamina = new StaminaModel();
        stamina.TryConsume(15f);

        stamina.Tick(1f);

        Assert.Equal(stamina.Max, stamina.Current);
    }
}
