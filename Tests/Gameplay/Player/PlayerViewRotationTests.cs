using Godot;
using GodotGameTemplate.Gameplay.Player;

namespace GodotGameTemplate.Tests.Gameplay.Player;

public sealed class PlayerViewRotationTests
{
    [Fact]
    public void ResolveRotation_RotatesFacingClockwiseByNinetyDegrees()
    {
        var rotation = PlayerViewRotation.ResolveRotation(Vector2.Right);

        Assert.Equal(Mathf.Pi / 2f, rotation);
    }

    [Fact]
    public void ResolveRotation_UpFacingKeepsArrowUnrotated()
    {
        var rotation = PlayerViewRotation.ResolveRotation(Vector2.Up);

        Assert.Equal(0f, rotation);
    }
}
