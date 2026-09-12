using GodotGameTemplate.Gameplay.Progression;

namespace GodotGameTemplate.Tests.Gameplay.Progression;

public sealed class PotionChargesModelTests
{
    [Fact]
    public void InitialState_FullCharges()
    {
        var potions = new PotionChargesModel();

        Assert.Equal(4, potions.MaxCharges);
        Assert.Equal(4, potions.Available);
    }

    [Fact]
    public void TryConsume_ReducesAvailable()
    {
        var potions = new PotionChargesModel();

        Assert.True(potions.TryConsume());

        Assert.Equal(3, potions.Available);
    }

    [Fact]
    public void TryConsume_WhenEmpty_ReturnsFalse()
    {
        var potions = new PotionChargesModel();

        for (var i = 0; i < 4; i++)
        {
            potions.TryConsume();
        }

        Assert.False(potions.TryConsume());
        Assert.Equal(0, potions.Available);
    }

    [Fact]
    public void Refill_RestoresAllCharges()
    {
        var potions = new PotionChargesModel();
        potions.TryConsume();
        potions.TryConsume();

        potions.Refill();

        Assert.Equal(4, potions.Available);
    }

    [Fact]
    public void Restore_ClampsToValidRange()
    {
        var potions = new PotionChargesModel();

        potions.Restore(99);

        Assert.Equal(4, potions.Available);

        potions.Restore(-3);

        Assert.Equal(0, potions.Available);
    }

    [Fact]
    public void CustomMaxCharges_Respected()
    {
        var potions = new PotionChargesModel(maxCharges: 2);

        Assert.Equal(2, potions.Available);
        potions.TryConsume();
        potions.TryConsume();
        Assert.False(potions.TryConsume());
    }
}
