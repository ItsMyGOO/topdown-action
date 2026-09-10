using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Tests.Gameplay.Skills;

public sealed class CooldownModelTests
{
    [Fact]
    public void IsReady_Default_IsTrue()
    {
        var cd = new CooldownModel();
        Assert.True(cd.IsReady(SkillSlot.Skill1));
        Assert.Equal(0f, cd.GetRemaining(SkillSlot.Skill1));
    }

    [Fact]
    public void Start_SetsRemaining()
    {
        var cd = new CooldownModel();
        cd.Start(SkillSlot.Skill1, 1.5f);
        Assert.False(cd.IsReady(SkillSlot.Skill1));
        Assert.True(cd.GetRemaining(SkillSlot.Skill1) > 0f);
    }

    [Fact]
    public void Tick_DecreasesAndClampsToZero()
    {
        var cd = new CooldownModel();
        cd.Start(SkillSlot.Skill1, 1f);

        cd.Tick(0.4f);

        Assert.InRange(cd.GetRemaining(SkillSlot.Skill1), 0.59f, 0.61f);

        cd.Tick(10f);

        Assert.Equal(0f, cd.GetRemaining(SkillSlot.Skill1));
        Assert.True(cd.IsReady(SkillSlot.Skill1));
    }
}
