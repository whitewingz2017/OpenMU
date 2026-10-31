namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
/// <summary>Editable from the Starting items by class plugin's configuration button.</summary>
public sealed class StartingItemsConfiguration
{
    public List<StartingClassItems> Classes { get; set; } = Enum.GetValues<StartingCharacterClass>().Select(c => new StartingClassItems { Class = c }).ToList();
}
