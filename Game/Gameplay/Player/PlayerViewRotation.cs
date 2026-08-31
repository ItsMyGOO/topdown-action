using Godot;

namespace GodotGameTemplate.Gameplay.Player;

public static class PlayerViewRotation
{
    public static float ResolveRotation(Vector2 facing)
    {
        return facing.Angle() + (Mathf.Pi / 2f);
    }
}
