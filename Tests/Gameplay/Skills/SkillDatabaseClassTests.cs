using GodotGameTemplate.Gameplay.Input.Commands;
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Tests.Gameplay.Skills;

public sealed class SkillDatabaseClassTests
{
    [Fact]
    public void SkillsFor_EveryClass_ProvidesAllSixSlots()
    {
        foreach (var classId in new[] { "barbarian", "sorcerer", "rogue" })
        {
            var skills = SkillDatabase.SkillsFor(classId);

            foreach (SkillSlot slot in Enum.GetValues(typeof(SkillSlot)))
            {
                Assert.True(skills.ContainsKey(slot), $"{classId} missing {slot}");
            }
        }
    }

    [Fact]
    public void SkillsFor_IdsCarryClassPrefix()
    {
        var barbarian = SkillDatabase.SkillsFor("barbarian");
        var sorcerer = SkillDatabase.SkillsFor("sorcerer");

        Assert.StartsWith(
            "barb_",
            barbarian[SkillSlot.Secondary].SkillId,
            StringComparison.Ordinal
        );
        Assert.StartsWith("mage_", sorcerer[SkillSlot.Secondary].SkillId, StringComparison.Ordinal);
        Assert.NotEqual(
            barbarian[SkillSlot.Secondary].SkillId,
            sorcerer[SkillSlot.Secondary].SkillId
        );
    }

    [Fact]
    public void SkillsFor_SorcererCostsMoreManaThanBarbarian()
    {
        var barbarian = SkillDatabase.SkillsFor("barbarian")[SkillSlot.Skill4];
        var sorcerer = SkillDatabase.SkillsFor("sorcerer")[SkillSlot.Skill4];

        Assert.True(sorcerer.ManaCost > barbarian.ManaCost);
    }

    [Fact]
    public void SkillsFor_BarbarianHasShorterRangeThanSorcerer()
    {
        var barbarian = SkillDatabase.SkillsFor("barbarian")[SkillSlot.Skill1];
        var sorcerer = SkillDatabase.SkillsFor("sorcerer")[SkillSlot.Skill1];

        Assert.True(sorcerer.Range > barbarian.Range);
    }

    [Fact]
    public void SkillsFor_RogueHasLowestCooldownsOnActives()
    {
        var rogueCooldown = SkillDatabase.SkillsFor("rogue")[SkillSlot.Skill4].CooldownSeconds;
        var barbarianCooldown = SkillDatabase
            .SkillsFor("barbarian")[SkillSlot.Skill4]
            .CooldownSeconds;

        Assert.True(rogueCooldown < barbarianCooldown);
    }

    [Fact]
    public void SkillsFor_PrimaryIsAlwaysFreeInstantMelee()
    {
        foreach (var classId in new[] { "barbarian", "sorcerer", "rogue" })
        {
            var primary = SkillDatabase.SkillsFor(classId)[SkillSlot.Primary];

            Assert.Equal(0f, primary.ManaCost);
            Assert.Equal(SkillEffectKind.None, primary.EffectKind);
        }
    }

    [Fact]
    public void SkillsFor_UnknownClass_FallsBackToBarbarian()
    {
        var fallback = SkillDatabase.SkillsFor("nope");
        var barbarian = SkillDatabase.SkillsFor("barbarian");

        Assert.Equal(barbarian[SkillSlot.Skill1].SkillId, fallback[SkillSlot.Skill1].SkillId);
    }
}
