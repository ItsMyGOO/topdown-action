using Godot;

namespace GodotGameTemplate.Gameplay.Input.Commands;

/// <summary>
/// 玩家层命令快照：表达“这一帧玩家想做什么”，不直接等价于设备输入。
/// </summary>
public readonly record struct PlayerCommand(
    Vector2? ClickMoveDestination,
    ulong? ClickTargetInstanceId,
    bool EvadePressed,
    bool InteractPressed,
    bool ToggleInventoryPressed,
    bool PrimaryPressed,
    bool SecondaryPressed,
    bool Skill1Pressed,
    bool Skill2Pressed,
    bool Skill3Pressed,
    bool Skill4Pressed
)
{
    /// <summary>
    /// 是否在任意一个技能槽上触发了按下事件。
    /// </summary>
    public bool AnySkillPressed =>
        PrimaryPressed
        || SecondaryPressed
        || Skill1Pressed
        || Skill2Pressed
        || Skill3Pressed
        || Skill4Pressed;
}
