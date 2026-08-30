using Godot;

namespace GodotGameTemplate.Gameplay.Actors;

public sealed class ActorContext
{
    public ActorIntent Intent { get; set; } = ActorIntent.None;

    public Vector2 Velocity { get; set; } = Vector2.Zero;

    public Vector2 Facing { get; set; } = Vector2.Down;

    public bool CanMove { get; set; } = true;

    public bool HasMoveInput => CanMove && Intent.HasMoveInput;
}
