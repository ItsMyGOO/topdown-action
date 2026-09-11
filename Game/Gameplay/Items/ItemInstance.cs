using System;
using System.Linq;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 物品槽位（暗黑式八槽）。只追加成员不改旧值，保证旧存档反序列化兼容。
/// </summary>
public enum ItemSlot
{
    Weapon,
    Armor,
    Accessory,
    Helmet,
    Gloves,
    Legs,
    Boots,
    Ring,
}

/// <summary>
/// 物品稀有度（首版三档）。
/// </summary>
public enum ItemRarity
{
    Common,
    Magic,
    Rare,
}

/// <summary>
/// 运行时物品实例（纯数据，不依赖 Godot Resource）。
/// </summary>
/// <param name="Id">物品标识（首版可用字符串常量）。</param>
/// <param name="Slot">装备槽位。</param>
/// <param name="Rarity">稀有度。</param>
/// <param name="Power">简化强度值（用于快速验证“换装有感”）。</param>
/// <param name="Affixes">随机词缀行；传 null 规范化为空，输入数组会被复制。</param>
public sealed class ItemInstance(
    string Id,
    ItemSlot Slot,
    ItemRarity Rarity,
    int Power,
    AffixLine[]? Affixes = null
) : IEquatable<ItemInstance>
{
    /// <summary>物品标识。</summary>
    public string Id { get; } = Id;

    /// <summary>装备槽位。</summary>
    public ItemSlot Slot { get; } = Slot;

    /// <summary>稀有度。</summary>
    public ItemRarity Rarity { get; } = Rarity;

    /// <summary>简化强度值。</summary>
    public int Power { get; } = Power;

    /// <summary>随机词缀行（只读，永不为 null）。</summary>
    public AffixLine[] Affixes { get; } = [.. (Affixes ?? Array.Empty<AffixLine>())];

    /// <summary>
    /// 值相等：逐字段 + 词条序列（顺序敏感）。
    /// </summary>
    public bool Equals(ItemInstance? other)
    {
        return other is not null
            && Id == other.Id
            && Slot == other.Slot
            && Rarity == other.Rarity
            && Power == other.Power
            && Affixes.SequenceEqual(other.Affixes);
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemInstance other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Slot);
        hash.Add(Rarity);
        hash.Add(Power);

        foreach (var affix in Affixes)
        {
            hash.Add(affix);
        }

        return hash.ToHashCode();
    }

    public override string ToString()
    {
        if (Affixes.Length == 0)
        {
            return $"{Id} [{Slot} P{Power}]";
        }

        var affixText = string.Join(", ", Affixes.Select(a => $"{a.Stat}+{a.Value:0.#}"));
        return $"{Id} [{Slot} P{Power}] ({affixText})";
    }
}
