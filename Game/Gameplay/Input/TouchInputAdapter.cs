using System.Collections.Generic;
using Godot;
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Input;

/// <summary>
/// 触屏输入适配占位：先暴露可由虚拟摇杆/按钮驱动的接口，后续 UI 可直接写入。
/// </summary>
public sealed class TouchInputAdapter : ICommandProvider
{
    private readonly Dictionary<SkillSlot, bool> _currentSkills = new();
    private readonly Dictionary<SkillSlot, bool> _previousSkills = new();

    private bool _evadePressed;
    private bool _previousEvadePressed;
    private bool _interactPressed;
    private bool _previousInteractPressed;
    private bool _toggleInventoryPressed;
    private bool _previousToggleInventoryPressed;
    private bool _confirmPressed;
    private bool _previousConfirmPressed;
    private bool _cancelPressed;
    private bool _previousCancelPressed;

    public Vector2 MoveVector { get; private set; }

    public Vector2 AimVector { get; private set; }

    public Vector2? ClickMoveDestination { get; private set; }

    public ulong? ClickTargetInstanceId { get; private set; }

    public void SetVirtualMove(Vector2 move)
    {
        MoveVector = move;
    }

    public void SetVirtualAim(Vector2 aim)
    {
        AimVector = aim;
    }

    public void SetSkillPressed(SkillSlot slot, bool pressed)
    {
        _currentSkills[slot] = pressed;
    }

    public void SetEvadePressed(bool pressed)
    {
        _evadePressed = pressed;
    }

    public void SetInteractPressed(bool pressed)
    {
        _interactPressed = pressed;
    }

    public void SetToggleInventoryPressed(bool pressed)
    {
        _toggleInventoryPressed = pressed;
    }

    public void SetConfirmPressed(bool pressed)
    {
        _confirmPressed = pressed;
    }

    public void SetCancelPressed(bool pressed)
    {
        _cancelPressed = pressed;
    }

    public void SetClickMoveDestination(Vector2? destination)
    {
        ClickMoveDestination = destination;
    }

    public void SetClickTargetInstanceId(ulong? instanceId)
    {
        ClickTargetInstanceId = instanceId;
    }

    public PlayerCommand GetCommand()
    {
        var command = new PlayerCommand(
            ClickMoveDestination: ClickMoveDestination,
            ClickTargetInstanceId: ClickTargetInstanceId,
            AimVector: AimVector,
            EvadePressed: JustPressed(_evadePressed, ref _previousEvadePressed),
            InteractPressed: JustPressed(_interactPressed, ref _previousInteractPressed),
            ToggleInventoryPressed: JustPressed(
                _toggleInventoryPressed,
                ref _previousToggleInventoryPressed
            ),
            ConfirmPressed: JustPressed(_confirmPressed, ref _previousConfirmPressed),
            CancelPressed: JustPressed(_cancelPressed, ref _previousCancelPressed),
            PrimaryPressed: IsSkillJustPressed(SkillSlot.Primary),
            SecondaryPressed: IsSkillJustPressed(SkillSlot.Secondary),
            Skill1Pressed: IsSkillJustPressed(SkillSlot.Skill1),
            Skill2Pressed: IsSkillJustPressed(SkillSlot.Skill2),
            Skill3Pressed: IsSkillJustPressed(SkillSlot.Skill3),
            Skill4Pressed: IsSkillJustPressed(SkillSlot.Skill4)
        );

        foreach (var pair in _currentSkills)
        {
            _previousSkills[pair.Key] = pair.Value;
        }

        return command;
    }

    private bool IsSkillJustPressed(SkillSlot slot)
    {
        var isPressed = _currentSkills.TryGetValue(slot, out var current) && current;
        var wasPressed = _previousSkills.TryGetValue(slot, out var previous) && previous;
        return isPressed && !wasPressed;
    }

    private static bool JustPressed(bool current, ref bool previous)
    {
        var justPressed = current && !previous;
        previous = current;
        return justPressed;
    }
}
