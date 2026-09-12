using System;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Progression;
using GodotGameTemplate.Gameplay.Progression.Paragon;
using GodotGameTemplate.Gameplay.Progression.Talents;

namespace GodotGameTemplate.Gameplay.Session;

/// <summary>
/// 新游戏重置规则（纯逻辑）。
/// <para>
/// <see cref="GameSession"/> 是 Godot 节点，无法在单测进程中构造，
/// 因此重置规则独立在此，测试只操作纯逻辑模型。
/// </para>
/// </summary>
public static class SessionReset
{
    /// <summary>
    /// 清空全部进度并返回应写入的职业 Id。
    /// </summary>
    /// <param name="setGold">金币写入器（会话属性由调用方绑定）。</param>
    /// <param name="setWorldTier">世界等级写入器。</param>
    public static string NewGame(
        InventoryModel inventory,
        EquipmentModel equipment,
        InventoryModel stash,
        LevelingModel leveling,
        TalentModel talents,
        ParagonModel paragon,
        PotionChargesModel potions,
        Action<int> setGold,
        Action<int> setWorldTier,
        string classId
    )
    {
        ArgumentNullException.ThrowIfNull(setGold);
        ArgumentNullException.ThrowIfNull(setWorldTier);

        setGold(0);
        inventory.Clear();
        stash.Clear();
        equipment.Clear();
        leveling.Restore(1, 0);
        talents.Restore([]);
        paragon.Restore([]);
        potions.Refill();
        setWorldTier(1);

        return classId;
    }
}
