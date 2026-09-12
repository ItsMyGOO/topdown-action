using GodotGameTemplate.Gameplay.Progression;

namespace GodotGameTemplate.Tests.Gameplay.Progression;

public sealed class EvadeChargesModelTests
{
    [Fact]
    public void InitialState_AllChargesAvailable()
    {
        var evade = new EvadeChargesModel();

        Assert.Equal(2, evade.MaxCharges);
        Assert.Equal(2, evade.Available);
    }

    [Fact]
    public void TryConsume_ReducesAvailable()
    {
        var evade = new EvadeChargesModel();

        Assert.True(evade.TryConsume());

        Assert.Equal(1, evade.Available);
    }

    [Fact]
    public void TryConsume_WhenEmpty_ReturnsFalse()
    {
        var evade = new EvadeChargesModel();

        Assert.True(evade.TryConsume());
        Assert.True(evade.TryConsume());
        Assert.False(evade.TryConsume());
    }

    [Fact]
    public void Tick_PartialRecharge_IsNotConsumable()
    {
        var evade = new EvadeChargesModel();
        evade.TryConsume();
        evade.TryConsume();

        evade.Tick(4.9f);

        Assert.Equal(0, evade.Available);
    }

    [Fact]
    public void Tick_AfterFullRecharge_Consumable()
    {
        var evade = new EvadeChargesModel();
        evade.TryConsume();
        evade.TryConsume();

        evade.Tick(2.5f);
        evade.Tick(2.5f);

        Assert.Equal(1, evade.Available);
    }

    [Fact]
    public void Tick_ClampsAtMaxCharges()
    {
        var evade = new EvadeChargesModel();

        evade.Tick(999f);

        Assert.Equal(2, evade.Available);
    }

    [Fact]
    public void Tick_NonPositiveDelta_DoesNothing()
    {
        var evade = new EvadeChargesModel();
        evade.TryConsume();

        evade.Tick(0f);
        evade.Tick(-1f);

        Assert.Equal(1, evade.Available);
    }

    [Fact]
    public void CustomMaxAndRecharge_Respected()
    {
        var evade = new EvadeChargesModel(maxCharges: 3, rechargeSeconds: 4f);

        Assert.Equal(3, evade.Available);
        Assert.True(evade.TryConsume());
        Assert.True(evade.TryConsume());
        Assert.True(evade.TryConsume());
        Assert.False(evade.TryConsume());

        evade.Tick(4f);

        Assert.Equal(1, evade.Available);
    }
}
