using Godot;

namespace GodotGameTemplate.Config.Player;

public interface IPlayerAttackConfig
{
    float TotalDuration { get; }

    float HitboxStartTime { get; }

    float HitboxEndTime { get; }

    float AttackRange { get; }

    string AttackId { get; }
}

[GlobalClass]
public partial class PlayerAttackConfig : Resource, IPlayerAttackConfig
{
    [Export(PropertyHint.Range, "0.01,5.0,0.01,or_greater")]
    public float TotalDuration { get; set; } = 0.25f;

    [Export(PropertyHint.Range, "0.0,5.0,0.01,or_greater")]
    public float HitboxStartTime { get; set; } = 0.08f;

    [Export(PropertyHint.Range, "0.0,5.0,0.01,or_greater")]
    public float HitboxEndTime { get; set; } = 0.16f;

    [Export(PropertyHint.Range, "0.0,128.0,1.0,or_greater")]
    public float AttackRange { get; set; } = 18f;

    [Export]
    public string AttackId { get; set; } = "player_basic_slash";
}
