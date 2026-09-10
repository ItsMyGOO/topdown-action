using Godot;
using GodotGameTemplate.Gameplay.Items;

namespace GodotGameTemplate.Gameplay.Session;

/// <summary>
/// 跨场景的最小会话数据（通过 Autoload 常驻于 /root）。
/// 仅保存与“核心循环”相关的轻量数据：背包、装备、金币等。
/// </summary>
public partial class GameSession : Node
{
    public InventoryModel Inventory { get; } = new();

    public EquipmentModel Equipment { get; } = new();

    public int Gold { get; set; }
}
