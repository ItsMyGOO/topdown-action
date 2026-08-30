using Godot;

namespace GodotGameTemplate.Gameplay.Actors.Combat;

public readonly record struct HitContext(Node Source, Vector2 Direction, string AttackId);
