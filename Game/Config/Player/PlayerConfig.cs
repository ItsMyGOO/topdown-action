using Godot;

namespace GodotGameTemplate.Config.Player;

[GlobalClass]
public partial class PlayerConfig : Resource
{
    [Export]
    public float MoveSpeed { get; set; } = 120f;

    [Export]
    public float Acceleration { get; set; } = 900f;

    [Export]
    public float Deceleration { get; set; } = 1100f;
}
