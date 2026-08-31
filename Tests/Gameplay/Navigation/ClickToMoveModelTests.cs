using Godot;
using GodotGameTemplate.Gameplay.Navigation;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Navigation;

public sealed class ClickToMoveModelTests
{
    [Fact]
    public void IsArrived_TrueWhenWithinStopRadius()
    {
        var model = new ClickToMoveModel { StopRadius = 10f };
        model.SetDestination(new Vector2(100, 100));

        Assert.True(model.IsArrived(new Vector2(108, 100)));
    }
}
