using GodotGameTemplate.Gameplay.Progression.Talents;

namespace GodotGameTemplate.Tests.Gameplay.Progression.Talents;

public sealed class TalentDatabaseTests
{
    [Fact]
    public void Catalog_IsNotEmpty_WithUniqueIds()
    {
        Assert.NotEmpty(TalentDatabase.Catalog);

        var ids = TalentDatabase.Catalog.Select(t => t.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Catalog_EveryPrerequisiteReferencesExistingTalent()
    {
        foreach (var talent in TalentDatabase.Catalog)
        {
            if (talent.RequiresId == null)
            {
                continue;
            }

            var prerequisite = TalentDatabase.Get(talent.RequiresId);

            Assert.NotNull(prerequisite);
            Assert.InRange(talent.RequiresRank, 1, prerequisite!.MaxRank);
        }
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        Assert.Null(TalentDatabase.Get("nope"));
    }

    [Fact]
    public void Get_KnownId_ReturnsDefinition()
    {
        Assert.Equal(TalentDatabase.Might, TalentDatabase.Get(TalentDatabase.Might)!.Id);
    }
}
