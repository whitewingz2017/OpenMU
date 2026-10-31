namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using System.Text.Json;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Muun;

/// <summary>Registers existing imported companion items with Muun storage without recreating items.</summary>
public static class ImportedMuunItems
{
    /// <summary>Adds missing companion rules, preserving administrators' existing settings and item bonuses.</summary>
    public static void Register(GameConfiguration config, MuunConfiguration settings)
    {
        using var stream = typeof(ImportedMuunItems).Assembly.GetManifestResourceStream("FinalMuCosmeticSkins.json")
            ?? throw new InvalidOperationException("The FinalMU companion catalog is missing.");
        var catalog = JsonSerializer.Deserialize<Companion[]>(stream)
            ?? throw new InvalidOperationException("The FinalMU companion catalog is invalid.");
        var types = catalog.Where(p => p.Part == 8 && !p.Mount).Select(p => p.Group * 512 + p.Number).Append(13 * 512 + 200);
        foreach (var type in types)
        {
            if (settings.Pets.Any(p => p.ItemType == type)
                || !config.Items.Any(d => d.Group * 512 + d.Number == type))
            {
                continue;
            }

            // Existing purchased companions can be +15; keep their levels and selected bonuses intact.
            settings.Pets.Add(new MuunDefinition { ItemType = type, MaximumLevel = 15 });
        }
    }

    private sealed record Companion(byte Group, short Number, int Part, bool Mount);
}
