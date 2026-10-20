// <copyright file="PlugInResourceTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using MUnique.OpenMU.GameLogic.Properties;

/// <summary>
/// Verifies the localized display properties used by the admin plugin list and search index.
/// </summary>
[TestFixture]
public class PlugInResourceTests
{
    /// <summary>
    /// Resolves labels through DisplayAttribute, which requires public resource properties.
    /// </summary>
    /// <param name="cultureName">The admin panel language.</param>
    [TestCase("en")]
    [TestCase("de")]
    [TestCase("zh-CN")]
    public void GameLogicPluginLabelsResolve(string cultureName)
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var labels = typeof(PlugInResources).Assembly.GetExportedTypes()
                .Select(type => (Type: type, Display: type.GetCustomAttribute<DisplayAttribute>()))
                .Where(entry => entry.Display?.ResourceType == typeof(PlugInResources)).ToArray();
            Assert.That(labels, Is.Not.Empty);
            foreach (var (type, display) in labels)
            {
                Assert.That(display!.GetName(), Is.Not.Null.And.Not.Empty, type.FullName);
                Assert.That(display.GetDescription(), Is.Not.Null.And.Not.Empty, type.FullName);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }
}
