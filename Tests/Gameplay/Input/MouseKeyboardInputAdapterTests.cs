using System;
using GodotGameTemplate.Gameplay.Input;
using GodotGameTemplate.Gameplay.Input.Commands;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Input;

public sealed class MouseKeyboardInputAdapterTests
{
    [Fact]
    public void ICommandProvider_InterfaceExistsAndGetCommandReturnsPlayerCommand()
    {
        var gameAssembly = typeof(PlayerInputAdapter).Assembly;
        var providerType = gameAssembly.GetType(
            "GodotGameTemplate.Gameplay.Input.ICommandProvider"
        );

        Assert.NotNull(providerType);
        Assert.True(providerType!.IsInterface);

        var method = providerType.GetMethod("GetCommand");
        Assert.NotNull(method);
        Assert.Equal(typeof(PlayerCommand), method!.ReturnType);
    }

    [Fact]
    public void MouseKeyboardInputAdapter_ImplementsICommandProviderAndHasParameterlessCtor()
    {
        var gameAssembly = typeof(PlayerInputAdapter).Assembly;
        var providerType =
            gameAssembly.GetType("GodotGameTemplate.Gameplay.Input.ICommandProvider")
            ?? throw new InvalidOperationException(
                "ICommandProvider should exist in the game assembly."
            );

        var adapterType = gameAssembly.GetType(
            "GodotGameTemplate.Gameplay.Input.MouseKeyboardInputAdapter"
        );

        Assert.NotNull(adapterType);
        Assert.True(providerType.IsAssignableFrom(adapterType!));

        var parameterlessCtor = adapterType!.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(parameterlessCtor);
    }
}
