using Godot;
using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Game.Scenes.Items;

/// <summary>
/// 世界掉落物（可点击拾取）。
/// </summary>
public partial class LootPickup : Area2D
{
    /// <summary>
    /// 该掉落物承载的物品实例。
    /// </summary>
    public ItemInstance Item { get; set; } =
        new(Id: "weapon_sword_basic", Slot: ItemSlot.Weapon, Rarity: ItemRarity.Common, Power: 1);

    [Signal]
    public delegate void PickedEventHandler(LootPickup pickup);

    public override void _Ready()
    {
        AddToGroup("loot_pickup");
    }

    /// <summary>
    /// 被拾取：发出信号并从场景树移除。
    /// </summary>
    public void Pick()
    {
        EmitSignal(SignalName.Picked, this);
        QueueFree();
    }
}
