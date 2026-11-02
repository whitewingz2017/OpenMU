namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using System.ComponentModel.DataAnnotations;
using MUnique.OpenMU.DataModel.Composition;
/// <summary>Starting item policy for one base class. Disabled profiles keep the legacy grants.</summary>
public sealed class StartingClassItems
{
    public StartingCharacterClass Class { get; set; }
    public bool Enabled { get; set; }
    public bool ReplaceLegacyItems { get; set; } = true;
    /// <summary>Gets or sets the Zen added to the new character's inventory.</summary>
    [Range(0, int.MaxValue)]
    [Display(Name = nameof(MUnique.OpenMU.GameLogic.PlugIns.AdministrationResources.StartingZen), Description = nameof(MUnique.OpenMU.GameLogic.PlugIns.AdministrationResources.StartingZenDescription), ResourceType = typeof(MUnique.OpenMU.GameLogic.PlugIns.AdministrationResources))]
    public int StartingZen { get; set; }
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<StartingItemReward> Items { get; set; } = [];
    /// <inheritdoc />
    public override string ToString() => this.Class.ToString();
}
