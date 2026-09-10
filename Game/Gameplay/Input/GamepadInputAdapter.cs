using System;
using System.Collections.Generic;
using Godot;
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Input;

/// <summary>
/// 手柄输入适配：提供最小多平台接入。
/// <para>
/// 当前仅负责：
/// - 左摇杆 -> 连续方向输入
/// - A/X/Y/LB/RB -> 若干技能槽
/// - B -> 翻滚
/// </para>
/// </summary>
public sealed class GamepadInputAdapter : ICommandProvider
{
    private readonly Func<int?> _deviceProvider;
    private readonly Func<int, JoyAxis, float> _axisReader;
    private readonly Func<int, JoyButton, bool> _buttonReader;
    private readonly float _deadzone;
    private readonly Dictionary<JoyButton, bool> _previousButtons = new();

    public GamepadInputAdapter()
        : this(
            GetDefaultDevice,
            (device, axis) => Godot.Input.GetJoyAxis(device, axis),
            (device, button) => Godot.Input.IsJoyButtonPressed(device, button),
            0.2f
        ) { }

    internal GamepadInputAdapter(
        Func<int?> deviceProvider,
        Func<int, JoyAxis, float> axisReader,
        Func<int, JoyButton, bool> buttonReader,
        float deadzone
    )
    {
        _deviceProvider = deviceProvider;
        _axisReader = axisReader;
        _buttonReader = buttonReader;
        _deadzone = deadzone;
    }

    public Vector2 MoveVector { get; private set; }

    public Vector2 AimVector { get; private set; }

    public PlayerCommand GetCommand()
    {
        var device = _deviceProvider();
        if (!device.HasValue)
        {
            MoveVector = Vector2.Zero;
            AimVector = Vector2.Zero;
            ResetButtonState();
            return CreateEmptyCommand();
        }

        MoveVector = ReadMoveVector(device.Value);
        AimVector = ReadAimVector(device.Value);

        var currentButtons = new Dictionary<JoyButton, bool>
        {
            [JoyButton.A] = _buttonReader(device.Value, JoyButton.A),
            [JoyButton.B] = _buttonReader(device.Value, JoyButton.B),
            [JoyButton.X] = _buttonReader(device.Value, JoyButton.X),
            [JoyButton.Y] = _buttonReader(device.Value, JoyButton.Y),
            [JoyButton.LeftShoulder] = _buttonReader(device.Value, JoyButton.LeftShoulder),
            [JoyButton.RightShoulder] = _buttonReader(device.Value, JoyButton.RightShoulder),
        };

        var command = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: AimVector,
            EvadePressed: JustPressed(JoyButton.B, currentButtons),
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: JustPressed(JoyButton.A, currentButtons),
            CancelPressed: JustPressed(JoyButton.B, currentButtons),
            PrimaryPressed: JustPressed(JoyButton.A, currentButtons),
            SecondaryPressed: JustPressed(JoyButton.X, currentButtons),
            Skill1Pressed: JustPressed(JoyButton.Y, currentButtons),
            Skill2Pressed: JustPressed(JoyButton.LeftShoulder, currentButtons),
            Skill3Pressed: JustPressed(JoyButton.RightShoulder, currentButtons),
            Skill4Pressed: false
        );

        foreach (var pair in currentButtons)
        {
            _previousButtons[pair.Key] = pair.Value;
        }

        return command;
    }

    private bool JustPressed(JoyButton button, IReadOnlyDictionary<JoyButton, bool> currentButtons)
    {
        var isPressed = currentButtons.TryGetValue(button, out var value) && value;
        var wasPressed = _previousButtons.TryGetValue(button, out var previous) && previous;
        return isPressed && !wasPressed;
    }

    private Vector2 ReadMoveVector(int device)
    {
        var move = new Vector2(
            ApplyDeadzone(_axisReader(device, JoyAxis.LeftX)),
            ApplyDeadzone(_axisReader(device, JoyAxis.LeftY))
        );

        return move.LengthSquared() > 1f ? move.Normalized() : move;
    }

    private Vector2 ReadAimVector(int device)
    {
        var aim = new Vector2(
            ApplyDeadzone(_axisReader(device, JoyAxis.RightX)),
            ApplyDeadzone(_axisReader(device, JoyAxis.RightY))
        );

        return aim.LengthSquared() > 1f ? aim.Normalized() : aim;
    }

    private float ApplyDeadzone(float value)
    {
        return Mathf.Abs(value) < _deadzone ? 0f : value;
    }

    private void ResetButtonState()
    {
        _previousButtons.Clear();
    }

    private static PlayerCommand CreateEmptyCommand()
    {
        return new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: Vector2.Zero,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: false,
            Skill1Pressed: false,
            Skill2Pressed: false,
            Skill3Pressed: false,
            Skill4Pressed: false
        );
    }

    private static int? GetDefaultDevice()
    {
        var connected = Godot.Input.GetConnectedJoypads();
        return connected.Count > 0 ? connected[0] : null;
    }
}
