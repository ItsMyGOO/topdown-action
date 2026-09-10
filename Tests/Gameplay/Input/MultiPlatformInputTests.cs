using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Input;
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Tests.Gameplay.Input;

public sealed class MultiPlatformInputTests
{
    [Fact]
    public void GamepadInputAdapter_MapsAxisAndButtonsToCommandSnapshot()
    {
        var adapterType = RequireType("GodotGameTemplate.Gameplay.Input.GamepadInputAdapter");
        var moveProperty = adapterType.GetProperty(
            "MoveVector",
            BindingFlags.Instance | BindingFlags.Public
        );
        Assert.NotNull(moveProperty);

        var axes = new Dictionary<JoyAxis, float>
        {
            [JoyAxis.LeftX] = 0.75f,
            [JoyAxis.LeftY] = -0.5f,
        };

        var buttons = new Dictionary<JoyButton, bool>
        {
            [JoyButton.A] = true,
            [JoyButton.B] = true,
            [JoyButton.Y] = true,
            [JoyButton.LeftShoulder] = true,
        };

        Func<int?> deviceProvider = () => 1;
        Func<int, JoyAxis, float> axisReader = (_, axis) =>
            axes.TryGetValue(axis, out var value) ? value : 0f;
        Func<int, JoyButton, bool> buttonReader = (_, button) =>
            buttons.TryGetValue(button, out var value) && value;

        var ctor = adapterType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            new[]
            {
                typeof(Func<int?>),
                typeof(Func<int, JoyAxis, float>),
                typeof(Func<int, JoyButton, bool>),
                typeof(float),
            },
            modifiers: null
        );

        Assert.NotNull(ctor);

        var adapter = ctor!.Invoke(new object[] { deviceProvider, axisReader, buttonReader, 0.2f });
        var getCommand = adapterType.GetMethod("GetCommand");
        Assert.NotNull(getCommand);

        var first = Assert.IsType<PlayerCommand>(
            getCommand!.Invoke(adapter, Array.Empty<object>())
        );
        Assert.Equal(new Vector2(0.75f, -0.5f), (Vector2)moveProperty!.GetValue(adapter)!);
        Assert.True(first.PrimaryPressed);
        Assert.True(first.EvadePressed);
        Assert.True(first.Skill1Pressed);
        Assert.True(first.Skill2Pressed);
        Assert.False(first.SecondaryPressed);
        Assert.False(first.Skill3Pressed);

        var second = Assert.IsType<PlayerCommand>(
            getCommand.Invoke(adapter, Array.Empty<object>())
        );
        Assert.False(second.PrimaryPressed);
        Assert.False(second.EvadePressed);
        Assert.False(second.Skill1Pressed);
        Assert.False(second.Skill2Pressed);
    }

    [Fact]
    public void TouchInputAdapter_SnapshotsVirtualMoveAndButtonEdges()
    {
        var adapterType = RequireType("GodotGameTemplate.Gameplay.Input.TouchInputAdapter");
        var moveProperty = adapterType.GetProperty(
            "MoveVector",
            BindingFlags.Instance | BindingFlags.Public
        );
        Assert.NotNull(moveProperty);

        var adapter = Activator.CreateInstance(adapterType);
        Assert.NotNull(adapter);

        adapterType
            .GetMethod("SetVirtualMove")!
            .Invoke(adapter, new object[] { new Vector2(0.4f, -1f) });
        adapterType
            .GetMethod("SetSkillPressed")!
            .Invoke(adapter, new object[] { SkillSlot.Secondary, true });
        adapterType
            .GetMethod("SetSkillPressed")!
            .Invoke(adapter, new object[] { SkillSlot.Skill4, true });
        adapterType.GetMethod("SetEvadePressed")!.Invoke(adapter, new object[] { true });

        var getCommand = adapterType.GetMethod("GetCommand");
        Assert.NotNull(getCommand);

        var first = Assert.IsType<PlayerCommand>(
            getCommand!.Invoke(adapter, Array.Empty<object>())
        );
        Assert.Equal(new Vector2(0.4f, -1f), (Vector2)moveProperty!.GetValue(adapter)!);
        Assert.True(first.SecondaryPressed);
        Assert.True(first.Skill4Pressed);
        Assert.True(first.EvadePressed);

        var second = Assert.IsType<PlayerCommand>(
            getCommand.Invoke(adapter, Array.Empty<object>())
        );
        Assert.False(second.SecondaryPressed);
        Assert.False(second.Skill4Pressed);
        Assert.False(second.EvadePressed);

        adapterType
            .GetMethod("SetSkillPressed")!
            .Invoke(adapter, new object[] { SkillSlot.Secondary, false });
        _ = getCommand.Invoke(adapter, Array.Empty<object>());
        adapterType
            .GetMethod("SetSkillPressed")!
            .Invoke(adapter, new object[] { SkillSlot.Secondary, true });

        var third = Assert.IsType<PlayerCommand>(getCommand.Invoke(adapter, Array.Empty<object>()));
        Assert.True(third.SecondaryPressed);
    }

    [Fact]
    public void PlayerController_MergeCommands_OrsFlagsAndKeepsFirstClickPayload()
    {
        var method = RequirePlayerControllerMethod(
            "MergeCommands",
            typeof(PlayerCommand),
            typeof(PlayerCommand),
            typeof(PlayerCommand)
        );

        var mouse = new PlayerCommand(
            ClickMoveDestination: new Vector2(10f, 20f),
            ClickTargetInstanceId: 99ul,
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
        var gamepad = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: new Vector2(0.3f, 0.4f),
            EvadePressed: true,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: true,
            SecondaryPressed: false,
            Skill1Pressed: false,
            Skill2Pressed: true,
            Skill3Pressed: false,
            Skill4Pressed: false
        );
        var touch = new PlayerCommand(
            ClickMoveDestination: new Vector2(-5f, -8f),
            ClickTargetInstanceId: 7ul,
            AimVector: new Vector2(0.9f, 0.1f),
            EvadePressed: false,
            InteractPressed: true,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: true,
            Skill1Pressed: false,
            Skill2Pressed: false,
            Skill3Pressed: true,
            Skill4Pressed: false
        );

        var merged = Assert.IsType<PlayerCommand>(
            method.Invoke(null, new object[] { mouse, gamepad, touch })
        );
        Assert.Equal(new Vector2(10f, 20f), merged.ClickMoveDestination);
        Assert.Equal(99ul, merged.ClickTargetInstanceId);
        Assert.True(merged.EvadePressed);
        Assert.True(merged.InteractPressed);
        Assert.True(merged.PrimaryPressed);
        Assert.True(merged.SecondaryPressed);
        Assert.True(merged.Skill2Pressed);
        Assert.True(merged.Skill3Pressed);
        Assert.Equal(new Vector2(0.3f, 0.4f), merged.AimVector);
    }

    [Fact]
    public void PlayerController_ResolveManualIntent_PrefersKeyboardThenGamepadThenTouch_AndSuppressesConsumedClicks()
    {
        var method = RequirePlayerControllerMethod(
            "ResolveManualIntent",
            typeof(PlayerCommand),
            typeof(Vector2),
            typeof(Vector2),
            typeof(Vector2),
            typeof(bool),
            typeof(bool)
        );

        var command = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            AimVector: Vector2.Zero,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            ConfirmPressed: false,
            CancelPressed: false,
            PrimaryPressed: true,
            SecondaryPressed: false,
            Skill1Pressed: false,
            Skill2Pressed: false,
            Skill3Pressed: false,
            Skill4Pressed: false
        );

        var keyboardFirst = Assert.IsType<ActorIntent>(
            method.Invoke(
                null,
                new object[]
                {
                    command,
                    new Vector2(1f, 0f),
                    new Vector2(0f, -1f),
                    new Vector2(-1f, 0f),
                    false,
                    false,
                }
            )
        );
        Assert.Equal(new Vector2(1f, 0f), keyboardFirst.Move);
        Assert.True(keyboardFirst.AttackPressed);

        var gamepadFallback = Assert.IsType<ActorIntent>(
            method.Invoke(
                null,
                new object[]
                {
                    command,
                    Vector2.Zero,
                    new Vector2(0f, -1f),
                    new Vector2(-1f, 0f),
                    false,
                    false,
                }
            )
        );
        Assert.Equal(new Vector2(0f, -1f), gamepadFallback.Move);

        var suppressed = Assert.IsType<ActorIntent>(
            method.Invoke(
                null,
                new object[]
                {
                    command,
                    Vector2.Zero,
                    Vector2.Zero,
                    new Vector2(-1f, 0f),
                    false,
                    true,
                }
            )
        );
        Assert.Equal(new Vector2(-1f, 0f), suppressed.Move);
        Assert.False(suppressed.AttackPressed);
    }

    private static Type RequireType(string fullName)
    {
        var type = typeof(PlayerInputAdapter).Assembly.GetType(fullName);
        Assert.NotNull(type);
        return type!;
    }

    private static MethodInfo RequirePlayerControllerMethod(
        string name,
        params Type[] parameterTypes
    )
    {
        var type = RequireType("GodotGameTemplate.Gameplay.Player.PlayerController");
        var method = type.GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: parameterTypes,
            modifiers: null
        );

        Assert.NotNull(method);
        return method!;
    }
}
