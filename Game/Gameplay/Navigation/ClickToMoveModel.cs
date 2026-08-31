using Godot;

namespace GodotGameTemplate.Gameplay.Navigation;

/// <summary>
/// 点击移动领域模型（纯逻辑，不依赖 Godot Node）：
/// 维护目标点，并提供“是否已到达”的判定。
/// </summary>
public sealed class ClickToMoveModel
{
    public Vector2? Destination { get; private set; }

    public float StopRadius { get; set; } = 6f;

    public void SetDestination(Vector2 destination) => Destination = destination;

    public void ClearDestination() => Destination = null;

    public bool IsArrived(Vector2 currentPosition)
    {
        if (Destination == null)
        {
            return true;
        }

        return currentPosition.DistanceTo(Destination.Value) <= StopRadius;
    }
}
