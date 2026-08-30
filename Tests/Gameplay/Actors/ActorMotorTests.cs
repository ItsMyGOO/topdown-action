using Godot;
using GodotGameTemplate.Gameplay.Actors;

namespace GodotGameTemplate.Tests.Gameplay.Actors;

public sealed class ActorMotorTests
{
    [Fact]
    public void UpdateVelocity_NoInput_DeceleratesTowardZero()
    {
        var velocity = new Vector2(100f, 0f);

        var next = ActorMotor.UpdateVelocity(
            currentVelocity: velocity,
            moveInput: Vector2.Zero,
            maxSpeed: 120f,
            acceleration: 600f,
            deceleration: 800f,
            delta: 0.1f
        );

        Assert.True(next.X < velocity.X);
        Assert.Equal(0f, next.Y);
    }

    [Fact]
    public void UpdateVelocity_DiagonalInput_CapsSpeedToMaxSpeed()
    {
        var next = ActorMotor.UpdateVelocity(
            currentVelocity: Vector2.Zero,
            moveInput: new Vector2(1f, 1f),
            maxSpeed: 120f,
            acceleration: 1200f,
            deceleration: 800f,
            delta: 1f
        );

        Assert.True(next.Length() <= 120.01f);
    }
}
