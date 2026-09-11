using System;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 掉落抽取（纯逻辑）：
/// 全槽位随机、稀有度概率 70/25/5、强度按稀有度、词缀按稀有度预算生成。
/// </summary>
public sealed class LootDropper
{
    private readonly Random _random;

    public LootDropper(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public ItemInstance RollBasicDrop()
    {
        return RollDrop(ItemRarity.Common);
    }

    /// <summary>
    /// 掷一次掉落；稀有度抽取带下限钳制（词缀预算跟随最终稀有度）。
    /// </summary>
    public ItemInstance RollDrop(ItemRarity rarityFloor)
    {
        var rarity = _random.NextDouble() switch
        {
            < 0.70 => ItemRarity.Common,
            < 0.95 => ItemRarity.Magic,
            _ => ItemRarity.Rare,
        };

        if (rarity < rarityFloor)
        {
            rarity = rarityFloor;
        }

        var power = rarity switch
        {
            ItemRarity.Rare => 5,
            ItemRarity.Magic => 3,
            _ => 1,
        };

        var slots = EquipmentModel.AllSlots;
        var slot = slots[_random.Next(slots.Count)];
        var affixes = AffixTable.Roll(rarity, _random);

        return new ItemInstance(
            Id: $"loot_{slot.ToString().ToLowerInvariant()}",
            Slot: slot,
            Rarity: rarity,
            Power: power,
            Affixes: affixes
        );
    }
}
