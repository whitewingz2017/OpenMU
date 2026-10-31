namespace MUnique.OpenMU.Persistence.Initialization.Updates;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;
[PlugIn]
[Display(Name = "Muun Companions", Description = "Separate Muun collection, two companions, bonuses and progression.")]
[Guid("a4b776a4-7d96-4920-9c31-5a878d38ce18")]
public sealed class MuunItemsUpdatePlugIn : UpdatePlugInBase
{
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;
    public override string Name => "Muun Companions";
    public override string Description => "Adds Muun items and registers the existing imported pet companions for Muun storage, preserving item IDs and bonuses. Apply with players offline.";
    public override DateTime CreatedAt => new(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc);
    public override int Version => 2;
    public override DateTime UpdatedAt => new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
    public override bool IsMandatory => false;
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration config)
    {
        VersionSeasonSix.Items.MuunItems.Ensure(context, config);
        var feature = config.PlugInConfigurations.SingleOrDefault(p => p.TypeId == typeof(GameLogic.Muun.MuunFeaturePlugIn).GUID);
        if (feature is null) { feature = context.CreateNew<PlugInConfiguration>(); feature.TypeId = typeof(GameLogic.Muun.MuunFeaturePlugIn).GUID; feature.CustomConfiguration = System.Text.Json.JsonSerializer.Serialize(new GameLogic.Muun.MuunConfiguration()); config.PlugInConfigurations.Add(feature); }
        var settings = System.Text.Json.JsonSerializer.Deserialize<GameLogic.Muun.MuunConfiguration>(feature.CustomConfiguration ?? "null")
            ?? new GameLogic.Muun.MuunConfiguration();
        VersionSeasonSix.Items.ImportedMuunItems.Register(config, settings);
        feature.CustomConfiguration = System.Text.Json.JsonSerializer.Serialize(settings);
        feature.IsActive = true;
        return ValueTask.CompletedTask;
    }
}
