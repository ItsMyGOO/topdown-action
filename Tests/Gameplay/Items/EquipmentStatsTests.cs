using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class EquipmentStatsTests
{
    [Fact]
    public void Summarize_WithEmptyEquipment_ReturnsZeroBonuses()
    {
        var summary = EquipmentStats.Summarize(new EquipmentModel());

        Assert.Equal(0f, summary.MaxManaBonus);
        Assert.Equal(0f, summary.MaxStaminaBonus);
        Assert.Equal(1f, summary.XpMultiplier);
    }

    [Fact]
    public void Summarize_SumsAffixesAcrossAllEquippedSlots()
    {
        var equipment = new EquipmentModel();
        equipment.Equip(
            new ItemInstance(
                "helmet",
                ItemSlot.Helmet,
                ItemRarity.Magic,
                2,
                [new AffixLine(AffixStat.BonusMaxMana, 10f)]
            )
        );
        equipment.Equip(
            new ItemInstance(
                "boots",
                ItemSlot.Boots,
                ItemRarity.Rare,
                4,
                [
                    new AffixLine(AffixStat.BonusMaxMana, 5f),
                    new AffixLine(AffixStat.BonusMaxStamina, 15f),
                ]
            )
        );
        equipment.Equip(
            new ItemInstance(
                "ring",
                ItemSlot.Ring,
                ItemRarity.Rare,
                4,
                [new AffixLine(AffixStat.BonusXpPercent, 20f)]
            )
        );

        var summary = EquipmentStats.Summarize(equipment);

        Assert.Equal(15f, summary.MaxManaBonus);
        Assert.Equal(15f, summary.MaxStaminaBonus);
        Assert.Equal(1.2f, summary.XpMultiplier);
    }

    [Fact]
    public void Summarize_EmptySlotsAreIgnored()
    {
        var equipment = new EquipmentModel();
        equipment.Equip(
            new ItemInstance(
                "gloves",
                ItemSlot.Gloves,
                ItemRarity.Common,
                1,
                [new AffixLine(AffixStat.BonusMaxStamina, 8f)]
            )
        );

        var summary = EquipmentStats.Summarize(equipment);

        Assert.Equal(0f, summary.MaxManaBonus);
        Assert.Equal(8f, summary.MaxStaminaBonus);
    }

    [Fact]
    public void Summarize_ArmorSumsNonWeaponPowerOnly()
    {
        var equipment = new EquipmentModel();
        equipment.Equip(new ItemInstance("sword", ItemSlot.Weapon, ItemRarity.Rare, 5));
        equipment.Equip(new ItemInstance("chest", ItemSlot.Armor, ItemRarity.Magic, 3));
        equipment.Equip(new ItemInstance("helm", ItemSlot.Helmet, ItemRarity.Common, 1));
        equipment.Equip(new ItemInstance("boots", ItemSlot.Boots, ItemRarity.Rare, 5));

        var summary = EquipmentStats.Summarize(equipment);

        Assert.Equal(9, summary.Armor);
    }

    [Fact]
    public void Summarize_WithEmptyEquipment_ArmorIsZero()
    {
        Assert.Equal(0, EquipmentStats.Summarize(new EquipmentModel()).Armor);
    }
}
