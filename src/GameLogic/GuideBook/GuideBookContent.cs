// <copyright file="GuideBookContent.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

using System.Text.Encodings.Web;
using System.Text.Json;
using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The categories and pages of the Guide Book: the published guide which every player reads, or the draft
/// which game masters edit.
/// </summary>
/// <remarks>
/// The limits match the ones of the client (GameLogic/GuideBook/GuideData.h of MuMain), which refuses a
/// guide beyond them; the whole guide has to fit into one packet together with the
/// <see cref="GuideBookValues"/>.
/// </remarks>
public sealed class GuideBookContent : IValidatableObject
{
    /// <summary>The maximum number of categories.</summary>
    internal const int MaximumCategories = 32;

    /// <summary>The maximum number of pages.</summary>
    internal const int MaximumPages = 256;

    /// <summary>The maximum length of an identifier, in UTF-8 bytes.</summary>
    internal const int MaximumIdBytes = 48;

    /// <summary>The maximum length of a category name, in UTF-8 bytes.</summary>
    internal const int MaximumNameBytes = 48;

    /// <summary>The maximum length of a page title, in UTF-8 bytes.</summary>
    internal const int MaximumTitleBytes = 80;

    /// <summary>The maximum length of a page text, in UTF-8 bytes.</summary>
    internal const int MaximumBodyBytes = 8000;

    /// <summary>
    /// The maximum length of the whole guide as JSON, in bytes. A packet holds 65535 bytes; the rest of it is
    /// left to the values and boss rewards which are sent along.
    /// </summary>
    internal const int MaximumJsonBytes = 52000;

    /// <summary>
    /// The JSON options which measure the guide: UTF-8 text unescaped, as the client writes and measures it, so
    /// that a guide within the client's limits is within these, too.
    /// </summary>
    private static readonly JsonSerializerOptions MeasureOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Gets or sets the number of saves of the draft. A save names the revision it started from, so a game
    /// master doesn't overwrite the save of another one by accident.
    /// </summary>
    [Display(Name = nameof(GuideBookResources.Revision), ResourceType = typeof(GuideBookResources))]
    public int Revision { get; set; }

    /// <summary>Gets or sets the categories, in the order of the client's sidebar.</summary>
    [Display(Name = nameof(GuideBookResources.Categories), ResourceType = typeof(GuideBookResources))]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<GuideBookCategory> Categories { get; set; } = new List<GuideBookCategory>();

    /// <summary>Gets or sets the pages, in the order of the client's sidebar.</summary>
    [Display(Name = nameof(GuideBookResources.Pages), ResourceType = typeof(GuideBookResources))]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<GuideBookPage> Pages { get; set; } = new List<GuideBookPage>();

    /// <summary>Creates a copy which shares no category or page with this content.</summary>
    /// <returns>The copy.</returns>
    public GuideBookContent Clone() => new()
    {
        Revision = this.Revision,
        Categories = this.Categories.Select(category => new GuideBookCategory { Id = category.Id, Name = category.Name }).ToList(),
        Pages = this.Pages.Select(page => new GuideBookPage { Id = page.Id, CategoryId = page.CategoryId, Title = page.Title, Body = page.Body }).ToList(),
    };

    /// <summary>
    /// Determines whether the client can show the guide: within the limits, unique identifiers of letters,
    /// digits, '-' and '_', a name or title everywhere, and every page in one of the categories.
    /// </summary>
    /// <returns><c>true</c>, if the guide is valid.</returns>
    public bool IsValid()
        => this.Categories is { Count: <= MaximumCategories } categories
            && this.Pages is { Count: <= MaximumPages } pages
            && categories.All(category => category is not null && IsId(category.Id) && IsText(category.Name, MaximumNameBytes))
            && pages.All(page => page is not null && IsId(page.Id) && IsText(page.Title, MaximumTitleBytes) && IsBody(page.Body))
            && categories.Select(category => category.Id).Distinct(StringComparer.Ordinal).Count() == categories.Count
            && pages.Select(page => page.Id).Distinct(StringComparer.Ordinal).Count() == pages.Count
            && pages.All(page => categories.Any(category => category.Id == page.CategoryId))
            && JsonSerializer.SerializeToUtf8Bytes(this, MeasureOptions).Length <= MaximumJsonBytes;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!this.IsValid())
        {
            yield return new ValidationResult(GuideBookResources.InvalidContent, [nameof(this.Pages)]);
        }
    }

    private static bool IsId(string? id)
        => !string.IsNullOrEmpty(id)
           && Encoding.UTF8.GetByteCount(id) <= MaximumIdBytes
           && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static bool IsText(string? text, int maximumBytes)
        => !string.IsNullOrWhiteSpace(text) && Encoding.UTF8.GetByteCount(text) <= maximumBytes;

    private static bool IsBody(string? body)
        => body is not null && Encoding.UTF8.GetByteCount(body) <= MaximumBodyBytes;
}
