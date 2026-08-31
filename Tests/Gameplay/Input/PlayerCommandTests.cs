using Godot;
using GodotGameTemplate.Gameplay.Input.Commands;
using Xunit;

namespace GodotGameTemplate.Tests.Gameplay.Input;

public sealed class PlayerCommandTests
{
    [Fact]
    public void AnySkillPressed_TrueWhenAnySkillIsPressed()
    {
        var cmd = new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            EvadePressed: false,
            InteractPressed: false,
            ToggleInventoryPressed: false,
            PrimaryPressed: false,
            SecondaryPressed: false,
            Skill1Pressed: false,
            Skill2Pressed: false,
            Skill3Pressed: true,
            Skill4Pressed: false
        );

        Assert.True(cmd.AnySkillPressed);
    }
}
