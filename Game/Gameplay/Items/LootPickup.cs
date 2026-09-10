using Godot;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 世界掉落物（可点击拾取）。
/// </summary>
public partial class LootPickup : Area2D
{
    [Export]
    public string ItemId { get; set; } = "gold";

    [Export]
    public int Quantity { get; set; } = 1;

    public override void _Ready()
    {
        AddToGroup("loot_pickup");
    }

    public ItemStack ToItemStack() => new(ItemId, Quantity);
}
