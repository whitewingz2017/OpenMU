namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>Localized metadata for the starter-item and Zen configuration plugins.</summary>
public static class AdministrationResources
{
    /// <summary>Gets the StartingItemsName caption.</summary>
    public static string StartingItemsName => Properties.PlugInResources.ResourceManager.GetString("Admin_StartingItemsName", System.Globalization.CultureInfo.CurrentUICulture) ?? "Admin_StartingItemsName";
    /// <summary>Gets the StartingItemsDescription caption.</summary>
    public static string StartingItemsDescription => Properties.PlugInResources.ResourceManager.GetString("Admin_StartingItemsDescription", System.Globalization.CultureInfo.CurrentUICulture) ?? "Admin_StartingItemsDescription";
    /// <summary>Gets the ZenDropsName caption.</summary>
    public static string ZenDropsName => Properties.PlugInResources.ResourceManager.GetString("Admin_ZenDropsName", System.Globalization.CultureInfo.CurrentUICulture) ?? "Admin_ZenDropsName";
    /// <summary>Gets the ZenDropsDescription caption.</summary>
    public static string ZenDropsDescription => Properties.PlugInResources.ResourceManager.GetString("Admin_ZenDropsDescription", System.Globalization.CultureInfo.CurrentUICulture) ?? "Admin_ZenDropsDescription";
}
