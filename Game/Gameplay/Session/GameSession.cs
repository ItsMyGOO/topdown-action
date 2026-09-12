using Godot;
using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Progression;
using GodotGameTemplate.Gameplay.Progression.Classes;
using GodotGameTemplate.Gameplay.Progression.Talents;
using GodotGameTemplate.Gameplay.Save;
using GodotGameTemplate.Gameplay.Skills;

namespace GodotGameTemplate.Gameplay.Session;

/// <summary>
/// 跨场景的最小会话数据（通过 Autoload 常驻于 /root）。
/// 仅保存与“核心循环”相关的轻量数据：背包、装备、金币等。
/// </summary>
public partial class GameSession : Node
{
    public InventoryModel Inventory { get; } = new();

    public EquipmentModel Equipment { get; } = new();

    public ManaModel Mana { get; } = new();

    public CooldownModel Cooldowns { get; } = new();

    public LevelingModel Leveling { get; } = new();

    public TalentModel Talents { get; } = new();

    public PotionChargesModel Potions { get; } = new();

    public string ClassId { get; set; } = ClassDatabase.DefaultClassId;

    public ISaveService SaveService { get; set; } = new SaveService();

    public int Gold { get; set; }

    public override void _Ready()
    {
        if (SaveService.ExistsAtDefaultPath())
        {
            Load();
        }
    }

    public void Save()
    {
        SaveService.Save(ToSaveData());
    }

    public void Load()
    {
        if (!SaveService.ExistsAtDefaultPath())
        {
            return;
        }

        ApplySaveData(SaveService.Load());
    }

    public SaveData ToSaveData()
    {
        return SaveDataMapper.FromState(
            Gold,
            Inventory.Items,
            Equipment,
            Leveling,
            Talents,
            Potions
        );
    }

    public void ApplySaveData(SaveData data)
    {
        Gold = SaveDataMapper.ApplyToState(
            data,
            Inventory,
            Equipment,
            out var classId,
            Leveling,
            Talents,
            Potions
        );
        ClassId = classId;
    }
}
