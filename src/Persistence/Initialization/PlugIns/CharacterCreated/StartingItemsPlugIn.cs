namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;
/// <summary>Admin-configured, class-specific starting items; automatic placement prevents overlap.</summary>
[PlugIn, Guid("5c840612-c047-4881-a218-180f14d62df1")]
[Display(Name = nameof(AdministrationResources.StartingItemsName), Description = nameof(AdministrationResources.StartingItemsDescription), ResourceType = typeof(AdministrationResources))]
public sealed class StartingItemsPlugIn : ICharacterCreatedPlugIn, ISupportCustomConfiguration<StartingItemsConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    public StartingItemsConfiguration? Configuration { get; set; }
    public object CreateDefaultConfig() => new StartingItemsConfiguration();
    public static bool ReplacesLegacy(GameConfiguration config, byte? number)
    {
        var plugin = config.PlugInConfigurations.SingleOrDefault(p => p.IsActive && p.TypeId == typeof(StartingItemsPlugIn).GUID);
        return plugin?.GetConfiguration<StartingItemsConfiguration>(null)?.Classes.Any(c => c.Enabled && (byte)c.Class == number && c.ReplaceLegacyItems) == true;
    }
    public void CharacterCreated(Account account, Character character, IContext context, GameConfiguration config, ILogger logger)
    {
        var profiles = (this.Configuration ??= new()).Classes.Where(c => c.Enabled && (byte)c.Class == character.CharacterClass?.Number).ToArray();
        if (profiles.Length == 0) return;
        if (profiles.Length != 1) throw new InvalidOperationException("Duplicate starting-item class profiles.");
        var profile = profiles[0];
        var plannedStorage = new PlannedStorage();
        foreach (var existing in character.Inventory!.Items) plannedStorage.Items.Add(existing);
        var bag = new Storage(InventoryConstants.InventoryRows * InventoryConstants.RowSize, 0, InventoryConstants.EquippableSlotsCount, new ItemStorageAdapter(plannedStorage, InventoryConstants.EquippableSlotsCount, (byte)(InventoryConstants.InventoryRows * InventoryConstants.RowSize)));
        var planned = new List<Item>();
        foreach (var reward in profile.Items)
        {
            if (reward.Group > 15 || reward.Number is < 0 or > 511 || reward.Level > 15 || reward.Quantity is < 1 or > 64 || planned.Count + reward.Quantity > 64)
                throw new InvalidOperationException("Invalid starting item or total quantity exceeds 64.");
            var definition = config.Items.SingleOrDefault(i => i.Group == reward.Group && i.Number == reward.Number)
                ?? throw new InvalidOperationException($"Unknown starting item {reward.Group}:{reward.Number}.");
            for (var i = 0; i < reward.Quantity; i++)
            {
                var item = new Item { Definition = definition, Level = reward.Level, Durability = definition.Durability };
                if (!bag.AddItemAsync(item).GetAwaiter().GetResult()) throw new InvalidOperationException("Starting items do not fit in the new character's bag.");
                planned.Add(item);
            }
        }
        foreach (var item in planned)
        {
            var persisted = context.CreateNew<Item>();
            persisted.Definition = item.Definition; persisted.Level = item.Level; persisted.Durability = item.Durability; persisted.ItemSlot = item.ItemSlot;
            character.Inventory.Items.Add(persisted);
        }
    }
    private sealed class PlannedStorage : ItemStorage
    {
        public PlannedStorage() { this.Items = new List<Item>(); }
    }
}
