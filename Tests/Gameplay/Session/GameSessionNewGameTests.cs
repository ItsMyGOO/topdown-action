using GodotGameTemplate.Gameplay.Items;
using GodotGameTemplate.Gameplay.Progression;
using GodotGameTemplate.Gameplay.Progression.Classes;
using GodotGameTemplate.Gameplay.Progression.Paragon;
using GodotGameTemplate.Gameplay.Progression.Talents;
using GodotGameTemplate.Gameplay.Session;

namespace GodotGameTemplate.Tests.Gameplay.Session;

public sealed class GameSessionNewGameTests
{
    [Fact]
    public void NewGame_ResetsAllProgress_AndReturnsClassId()
    {
        var inventory = new InventoryModel();
        inventory.TryAdd(new ItemInstance("loot", ItemSlot.Weapon, ItemRarity.Rare, 5));
        var stash = new InventoryModel();
        stash.TryAdd(new ItemInstance("kept", ItemSlot.Armor, ItemRarity.Common, 1));
        var equipment = new EquipmentModel();
        equipment.Equip(new ItemInstance("worn", ItemSlot.Weapon, ItemRarity.Common, 1));
        var leveling = new LevelingModel();
        leveling.AddXp(120);
        var talents = new TalentModel();
        talents.Allocate(TalentDatabase.Might, 3);
        var paragon = new ParagonModel();
        paragon.Allocate(ParagonCategory.Brutality, 20);
        var potions = new PotionChargesModel();
        potions.TryConsume();
        potions.TryConsume();

        var gold = -1;
        var worldTier = -1;

        var classId = SessionReset.NewGame(
            inventory,
            equipment,
            stash,
            leveling,
            talents,
            paragon,
            potions,
            value => gold = value,
            value => worldTier = value,
            ClassDatabase.Rogue
        );

        Assert.Equal(ClassDatabase.Rogue, classId);
        Assert.Equal(0, gold);
        Assert.Equal(1, worldTier);
        Assert.Empty(inventory.Items);
        Assert.Empty(stash.Items);
        Assert.Null(equipment.Weapon);
        Assert.Equal(1, leveling.Level);
        Assert.Equal(0, leveling.CurrentXp);
        Assert.Empty(talents.Ranks);
        Assert.Empty(paragon.Ranks);
        Assert.Equal(potions.MaxCharges, potions.Available);
    }
}
