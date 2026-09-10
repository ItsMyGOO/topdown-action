namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 掉落抽取（首版极简：固定掉落一件武器，稀有度按概率抽取）。
/// </summary>
public sealed class LootDropper
{
    private readonly System.Random _random = new();

    public ItemInstance RollBasicDrop()
    {
        var rarity = _random.NextDouble() switch
        {
            < 0.70 => ItemRarity.Common,
            < 0.95 => ItemRarity.Magic,
            _ => ItemRarity.Rare,
        };

        var power = rarity switch
        {
            ItemRarity.Rare => 5,
            ItemRarity.Magic => 3,
            _ => 1,
        };

        return new ItemInstance(
            Id: "weapon_sword_basic",
            Slot: ItemSlot.Weapon,
            Rarity: rarity,
            Power: power
        );
    }
}
