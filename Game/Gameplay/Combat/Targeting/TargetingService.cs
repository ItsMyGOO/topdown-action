namespace GodotGameTemplate.Gameplay.Combat.Targeting;

/// <summary>
/// 目标锁定服务（纯逻辑，不依赖 Godot Node）。
/// </summary>
public sealed class TargetingService
{
    public ulong? CurrentTargetInstanceId { get; private set; }

    public void SetTarget(ulong instanceId) => CurrentTargetInstanceId = instanceId;

    public void ClearTarget() => CurrentTargetInstanceId = null;
}
