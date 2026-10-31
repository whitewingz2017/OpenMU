namespace MUnique.OpenMU.GameLogic.PlugIns;
using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;
/// <summary>Admin control of monster Zen amounts, independent of item-drop chances.</summary>
[PlugIn, Guid("bde5b131-d79b-4b38-a5cb-0995ec5344ae")]
[Display(Name = nameof(AdministrationResources.ZenDropsName), Description = nameof(AdministrationResources.ZenDropsDescription), ResourceType = typeof(AdministrationResources))]
public sealed class ZenDropPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<ZenDropConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    public ZenDropConfiguration? Configuration { get; set; }
    public object CreateDefaultConfig() => new ZenDropConfiguration();
    public static uint Apply(uint amount, ZenDropConfiguration? settings)
    {
        if (amount == 0 || settings is null) return amount;
        if (!double.IsFinite(settings.AmountMultiplier) || settings.AmountMultiplier is < 0 or > 10000 || settings.MinimumAmount < 0 || settings.MaximumAmount < settings.MinimumAmount)
            return amount;
        return (uint)Math.Clamp(Math.Floor(amount * settings.AmountMultiplier), settings.MinimumAmount, settings.MaximumAmount);
    }
}
