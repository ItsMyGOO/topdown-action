using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class AffixTableTests
{
    [Fact]
    public void Roll_CommonRarity_GrantsNoAffixes()
    {
        var random = new Random(1234);

        var affixes = AffixTable.Roll(ItemRarity.Common, random);

        Assert.Empty(affixes);
    }

    [Fact]
    public void Roll_MagicRarity_GrantsExactlyOneAffix()
    {
        var random = new Random(1234);

        for (var i = 0; i < 20; i++)
        {
            var affixes = AffixTable.Roll(ItemRarity.Magic, random);

            Assert.Single(affixes);
        }
    }

    [Fact]
    public void Roll_RareRarity_GrantsTwoDistinctAffixes()
    {
        var random = new Random(1234);

        for (var i = 0; i < 20; i++)
        {
            var affixes = AffixTable.Roll(ItemRarity.Rare, random);

            Assert.Equal(2, affixes.Length);
            Assert.NotEqual(affixes[0].Stat, affixes[1].Stat);
        }
    }

    [Fact]
    public void Roll_ValuesStayWithinConfiguredRanges()
    {
        var random = new Random(99);

        for (var i = 0; i < 100; i++)
        {
            foreach (var affix in AffixTable.Roll(ItemRarity.Rare, random))
            {
                switch (affix.Stat)
                {
                    case AffixStat.BonusMaxMana:
                    case AffixStat.BonusMaxStamina:
                        Assert.InRange(affix.Value, 5f, 15f);
                        break;
                    case AffixStat.BonusXpPercent:
                        Assert.InRange(affix.Value, 5f, 20f);
                        break;
                }
            }
        }
    }

    [Fact]
    public void Roll_SameSeed_ProducesSameSequence()
    {
        var a = AffixTable.Roll(ItemRarity.Rare, new Random(7));
        var b = AffixTable.Roll(ItemRarity.Rare, new Random(7));

        Assert.Equal(a, b);
    }
}
