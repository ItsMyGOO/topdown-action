namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 物品槽位（轻量三槽版本）。
/// </summary>
public enum ItemSlot
{
    Weapon,
    Armor,
    Accessory,
}

/// <summary>
/// 物品稀有度（首版仅三档）。
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
/// <param name="Slot">装备槽位（武器/护甲/饰品）。</param>
/// <param name="Rarity">稀有度。</param>
/// <param name="Power">简化强度值（用于快速验证“换装有感”）。</param>
public sealed record ItemInstance(string Id, ItemSlot Slot, ItemRarity Rarity, int Power);
