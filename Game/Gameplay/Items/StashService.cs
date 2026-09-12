using System.Linq;

namespace GodotGameTemplate.Gameplay.Items;

/// <summary>
/// 背包 ⇄ 仓库搬运规则（纯逻辑）：
/// 先扣源头、再进目标；目标装不下则整体回滚，两端无副作用。
/// </summary>
public static class StashService
{
    /// <summary>
    /// 从背包存入仓库。
    /// </summary>
    public static bool TryDeposit(InventoryModel inventory, InventoryModel stash, ItemInstance item)
    {
        if (!inventory.Items.Contains(item) || stash.Items.Count >= stash.Capacity)
        {
            return false;
        }

        inventory.Remove(item);
        stash.TryAdd(item);

        return true;
    }

    /// <summary>
    /// 从仓库取出到背包。
    /// </summary>
    public static bool TryWithdraw(
        InventoryModel stash,
        InventoryModel inventory,
        ItemInstance item
    )
    {
        if (!stash.Items.Contains(item) || inventory.Items.Count >= inventory.Capacity)
        {
            return false;
        }

        stash.Remove(item);
        inventory.TryAdd(item);

        return true;
    }
}
