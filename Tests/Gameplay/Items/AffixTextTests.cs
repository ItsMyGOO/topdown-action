using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Tests.Gameplay.Items;

public sealed class AffixTextTests
{
    [Fact]
    public void Format_MapsEachStatToChineseDescription()
    {
        Assert.Equal("+12 最大法力", AffixText.Format(new AffixLine(AffixStat.BonusMaxMana, 12f)));
        Assert.Equal("+9 护甲", AffixText.Format(new AffixLine(AffixStat.BonusArmor, 9f)));
        Assert.Equal(
            "+15% 经验获取",
            AffixText.Format(new AffixLine(AffixStat.BonusXpPercent, 15f))
        );
    }

    [Fact]
    public void FormatAll_JoinsWithChineseComma()
    {
        var text = AffixText.FormatAll([
            new AffixLine(AffixStat.BonusMaxMana, 5f),
            new AffixLine(AffixStat.BonusXpPercent, 10f),
        ]);

        Assert.Equal("+5 最大法力，+10% 经验获取", text);
    }
}
