using Godot;

namespace GodotGameTemplate.Gameplay.Actors;

public readonly record struct ActorIntent(Vector2 Move)
{
    public bool HasMoveInput => Move.LengthSquared() > 0f;

    public static ActorIntent None => new(Vector2.Zero);
}
