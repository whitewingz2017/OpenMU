namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using System.ComponentModel.DataAnnotations;
using MUnique.OpenMU.DataModel.Composition;
/// <summary>Editable from the Starting items by class plugin's configuration button.</summary>
public sealed class StartingItemsConfiguration
{
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<StartingClassItems> Classes { get; set; } = Enum.GetValues<StartingCharacterClass>().Select(c => new StartingClassItems { Class = c }).ToList();
}
