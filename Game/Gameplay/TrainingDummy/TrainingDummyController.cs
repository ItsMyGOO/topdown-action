using Godot;
using GodotGameTemplate.Gameplay.Actors.Combat;

namespace GodotGameTemplate.Gameplay.TrainingDummy;

public partial class TrainingDummyController : Node2D, IHitReceiver
{
    [Export]
    public Polygon2D? Body { get; set; }

    [Export]
    public float FlashDuration { get; set; } = 0.12f;

    private Color _defaultColor = Colors.White;
    private double _flashRemaining;

    public HitContext? LastHit { get; private set; }

    public override void _Ready()
    {
        Body ??= GetNodeOrNull<Polygon2D>("Body");
        if (Body != null)
        {
            _defaultColor = Body.Color;
        }
    }

    public override void _Process(double delta)
    {
        if (_flashRemaining <= 0d || Body == null)
        {
            return;
        }

        _flashRemaining -= delta;
        if (_flashRemaining <= 0d)
        {
            Body.Color = _defaultColor;
        }
    }

    public void ReceiveHit(HitContext hit)
    {
        LastHit = hit;
        _flashRemaining = FlashDuration;

        if (Body != null)
        {
            Body.Color = Colors.OrangeRed;
        }
    }
}
