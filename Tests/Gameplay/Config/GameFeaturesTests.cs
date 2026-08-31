using GodotGameTemplate.Config;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Config;

public sealed class GameFeaturesTests
{
    [Fact]
    public void Defaults_AreEnabled()
    {
        var features = new GameFeatures();
        Assert.True(features.EnableLoot);
        Assert.True(features.EnableInventory);
        Assert.True(features.EnableLeveling);
        Assert.True(features.EnableTown);
    }
}
