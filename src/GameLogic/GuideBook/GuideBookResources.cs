// <copyright file="GuideBookResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

using System.Globalization;

/// <summary>Provides localized metadata for the Guide Book plugins and their configuration.</summary>
public static class GuideBookResources
{
    /// <summary>Gets the feature name.</summary>
    public static string Name => Get("GuideBook_Name");

    /// <summary>Gets the feature description.</summary>
    public static string Description => Get("GuideBook_Description");

    /// <summary>Gets the caption of the published guide.</summary>
    public static string Published => Get("GuideBook_Published");

    /// <summary>Gets the caption of the draft.</summary>
    public static string Draft => Get("GuideBook_Draft");

    /// <summary>Gets the caption of the revision.</summary>
    public static string Revision => Get("GuideBook_Revision");

    /// <summary>Gets the caption of the categories.</summary>
    public static string Categories => Get("GuideBook_Categories");

    /// <summary>Gets the caption of the pages.</summary>
    public static string Pages => Get("GuideBook_Pages");

    /// <summary>Gets the caption of an identifier.</summary>
    public static string Id => Get("GuideBook_Id");

    /// <summary>Gets the caption of a category name.</summary>
    public static string CategoryName => Get("GuideBook_CategoryName");

    /// <summary>Gets the caption of the category of a page.</summary>
    public static string CategoryId => Get("GuideBook_CategoryId");

    /// <summary>Gets the caption of a page title.</summary>
    public static string Title => Get("GuideBook_Title");

    /// <summary>Gets the caption of a page text.</summary>
    public static string Body => Get("GuideBook_Body");

    /// <summary>Gets the validation message of a guide which the client can't show.</summary>
    public static string InvalidContent => Get("GuideBook_InvalidContent");

    private static string Get(string key) => PlugInResources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? string.Empty;
}
