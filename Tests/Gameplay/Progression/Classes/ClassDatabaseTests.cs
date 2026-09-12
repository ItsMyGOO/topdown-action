using GodotGameTemplate.Gameplay.Progression.Classes;

namespace GodotGameTemplate.Tests.Gameplay.Progression.Classes;

public sealed class ClassDatabaseTests
{
    [Fact]
    public void Catalog_HasThreeClasses_WithUniqueIds()
    {
        Assert.Equal(3, ClassDatabase.Catalog.Count);

        var ids = ClassDatabase.Catalog.Select(c => c.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Get_UnknownId_FallsBackToDefault()
    {
        var definition = ClassDatabase.Get("nope");

        Assert.Equal(ClassDatabase.DefaultClassId, definition.Id);
    }

    [Fact]
    public void Get_Barbarian_MatchesDesignMultipliers()
    {
        var barbarian = ClassDatabase.Get("barbarian");

        Assert.Equal(1.3f, barbarian.DamageMultiplier);
        Assert.Equal(1.25f, barbarian.HealthMultiplier);
        Assert.Equal(0.6f, barbarian.ManaMultiplier);
        Assert.Equal(1.0f, barbarian.MoveSpeedMultiplier);
    }

    [Fact]
    public void Get_Sorcerer_IsFragileCaster()
    {
        var sorcerer = ClassDatabase.Get("sorcerer");

        Assert.True(sorcerer.ManaMultiplier > 1f);
        Assert.True(sorcerer.HealthMultiplier < 1f);
    }

    [Fact]
    public void Get_Rogue_IsFasterThanOthers()
    {
        var rogue = ClassDatabase.Get("rogue");

        Assert.Equal(1.2f, rogue.MoveSpeedMultiplier);
        Assert.True(rogue.MoveSpeedMultiplier > ClassDatabase.Get("barbarian").MoveSpeedMultiplier);
    }
}
