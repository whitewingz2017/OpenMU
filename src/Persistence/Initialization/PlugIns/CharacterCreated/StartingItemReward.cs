namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using System.ComponentModel.DataAnnotations;
/// <summary>One plain starting item, placed automatically into the bag.</summary>
public sealed class StartingItemReward
{
    [Range(0, 15)] public byte Group { get; set; }
    [Range(0, 511)] public short Number { get; set; }
    [Range(0, 15)] public byte Level { get; set; }
    [Range(1, 64)] public int Quantity { get; set; } = 1;
    /// <inheritdoc />
    public override string ToString() => $"{this.Group}:{this.Number} +{this.Level} x{this.Quantity}";
}
