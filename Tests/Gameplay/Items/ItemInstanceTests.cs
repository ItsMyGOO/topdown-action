using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class ItemInstanceTests
{
    [Fact]
    public void Affixes_DefaultsToEmptyAndNeverNull()
    {
        var item = new ItemInstance("sword", ItemSlot.Weapon, ItemRarity.Common, 1);

        Assert.Empty(item.Affixes);
    }

    [Fact]
    public void Affixes_NullInputIsNormalizedToEmpty()
    {
        var item = new ItemInstance("sword", ItemSlot.Weapon, ItemRarity.Common, 1, Affixes: null);

        Assert.Empty(item.Affixes);
    }

    [Fact]
    public void Equality_IncludesAffixesAndIsOrderSensitive()
    {
        var a = new ItemInstance(
            "sword",
            ItemSlot.Weapon,
            ItemRarity.Rare,
            5,
            [new AffixLine(AffixStat.BonusMaxMana, 10f), new AffixLine(AffixStat.BonusArmor, 5f)]
        );
        var sameValues = new ItemInstance(
            "sword",
            ItemSlot.Weapon,
            ItemRarity.Rare,
            5,
            [new AffixLine(AffixStat.BonusMaxMana, 10f), new AffixLine(AffixStat.BonusArmor, 5f)]
        );
        var swapped = new ItemInstance(
            "sword",
            ItemSlot.Weapon,
            ItemRarity.Rare,
            5,
            [new AffixLine(AffixStat.BonusArmor, 5f), new AffixLine(AffixStat.BonusMaxMana, 10f)]
        );
        var noAffixes = new ItemInstance("sword", ItemSlot.Weapon, ItemRarity.Rare, 5);

        Assert.Equal(a, sameValues);
        Assert.NotEqual(a, swapped);
        Assert.NotEqual(a, noAffixes);
        Assert.Equal(a.GetHashCode(), sameValues.GetHashCode());
    }

    [Fact]
    public void Affixes_InputArrayIsCopied_Defensive()
    {
        var input = new AffixLine[] { new(AffixStat.BonusMaxMana, 10f) };
        var item = new ItemInstance("sword", ItemSlot.Weapon, ItemRarity.Magic, 3, input);

        input[0] = new AffixLine(AffixStat.BonusMaxMana, 99f);

        Assert.Equal(10f, item.Affixes[0].Value);
    }
}
