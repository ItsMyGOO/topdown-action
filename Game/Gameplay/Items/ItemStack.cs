namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 最小物品栈：只包含物品 Id 与数量。
/// <para>
/// 该类型为纯逻辑数据结构，不依赖 Godot。
/// </para>
/// </summary>
public readonly record struct ItemStack(string ItemId, int Quantity);
