using Godot;
using GodotGameTemplate.Gameplay.Actors;

namespace GodotGameTemplate.Gameplay.Player;

public partial class PlayerView : Node2D
{
    public void Sync(ActorContext context)
    {
        if (context.Facing == Vector2.Zero)
        {
            return;
        }

        Rotation = PlayerViewRotation.ResolveRotation(context.Facing);
    }
}
