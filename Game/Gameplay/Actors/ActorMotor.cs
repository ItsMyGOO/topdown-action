using Godot;

namespace GodotGameTemplate.Gameplay.Actors;

public static class ActorMotor
{
    public static Vector2 UpdateVelocity(
        Vector2 currentVelocity,
        Vector2 moveInput,
        float maxSpeed,
        float acceleration,
        float deceleration,
        float delta
    )
    {
        if (delta <= 0f)
        {
            return currentVelocity;
        }

        var direction = moveInput.LengthSquared() > 1f
            ? moveInput.Normalized()
            : moveInput;

        var targetVelocity = direction * maxSpeed;
        var rate = direction == Vector2.Zero ? deceleration : acceleration;

        return currentVelocity.MoveToward(targetVelocity, rate * delta);
    }
}
