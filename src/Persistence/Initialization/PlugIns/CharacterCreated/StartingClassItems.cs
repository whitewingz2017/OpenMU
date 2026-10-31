namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
/// <summary>Starting item policy for one base class. Disabled profiles keep the legacy grants.</summary>
public sealed class StartingClassItems
{
    public StartingCharacterClass Class { get; set; }
    public bool Enabled { get; set; }
    public bool ReplaceLegacyItems { get; set; } = true;
    public List<StartingItemReward> Items { get; set; } = [];
    /// <inheritdoc />
    public override string ToString() => this.Class.ToString();
}
