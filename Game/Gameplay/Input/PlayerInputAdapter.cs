using Godot;
using GodotGameTemplate.Gameplay.Actors;

namespace GodotGameTemplate.Gameplay.Input;

public sealed class PlayerInputAdapter : IIntentProvider
{
    public ActorIntent GetIntent()
    {
        var move = Godot.Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var attackPressed = Godot.Input.IsActionJustPressed("attack_primary");
        return new ActorIntent(move, attackPressed);
    }
}
