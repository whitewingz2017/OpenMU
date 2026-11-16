// <copyright file="GuideBookContentTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Text.Json;
using MUnique.OpenMU.GameLogic.GuideBook;

/// <summary>
/// Tests the <see cref="GuideBookContent"/>: the limits which the client can show, and the copy which is published.
/// </summary>
[TestFixture]
internal class GuideBookContentTests
{
    /// <summary>
    /// Tests that the guide which a new server starts with is valid, so the client shows it.
    /// </summary>
    [Test]
    public void DefaultGuideIsValid()
    {
        Assert.That(GuideBookDefaults.Create().IsValid(), Is.True);
    }

    /// <summary>
    /// Tests that the default guide only links to its own pages.
    /// </summary>
    [Test]
    public void DefaultGuideLinksToItsOwnPages()
    {
        var guide = GuideBookDefaults.Create();
        var linked = guide.Pages
            .SelectMany(page => page.Body.Split("[[").Skip(1))
            .Select(link => link[..link.IndexOfAny(['|', ']'])]);

        Assert.That(linked, Is.SubsetOf(guide.Pages.Select(page => page.Id)));
    }

    /// <summary>
    /// Tests that a page needs an existing category.
    /// </summary>
    [Test]
    public void PageOutsideOfTheCategoriesIsInvalid()
    {
        var guide = CreateGuide();
        guide.Pages.First().CategoryId = "cat-missing";

        Assert.That(guide.IsValid(), Is.False);
    }

    /// <summary>
    /// Tests that two pages can't share an identifier, because links address pages by it.
    /// </summary>
    [Test]
    public void RepeatedPageIdIsInvalid()
    {
        var guide = CreateGuide();
        guide.Pages.Add(new GuideBookPage { Id = "page-1", CategoryId = "cat-1", Title = "Copy" });

        Assert.That(guide.IsValid(), Is.False);
    }

    /// <summary>
    /// Tests that an identifier is restricted to the characters the client accepts.
    /// </summary>
    [Test]
    public void IdWithSpaceIsInvalid()
    {
        var guide = CreateGuide();
        guide.Categories.First().Id = "cat 1";
        guide.Pages.First().CategoryId = "cat 1";

        Assert.That(guide.IsValid(), Is.False);
    }

    /// <summary>
    /// Tests that a page text longer than the client reads is refused.
    /// </summary>
    [Test]
    public void TooLongBodyIsInvalid()
    {
        var guide = CreateGuide();
        guide.Pages.First().Body = new string('a', GuideBookContent.MaximumBodyBytes + 1);

        Assert.That(guide.IsValid(), Is.False);
    }

    /// <summary>
    /// Tests that the published copy doesn't change with the draft.
    /// </summary>
    [Test]
    public void CloneSharesNoPage()
    {
        var draft = CreateGuide();
        var published = draft.Clone();
        draft.Pages.First().Title = "Changed";

        Assert.That(published.Pages.First().Title, Is.EqualTo("Welcome"));
    }

    /// <summary>
    /// Tests that the JSON which the client sends for a save is read as a guide.
    /// </summary>
    [Test]
    public void ClientJsonIsRead()
    {
        const string json = """{"Revision":3,"Categories":[{"Id":"cat-1","Name":"Start"}],"Pages":[{"Id":"page-1","CategoryId":"cat-1","Title":"Hello","Body":"**Hi**"}]}""";

        var guide = JsonSerializer.Deserialize<GuideBookContent>(json)!;

        Assert.That(guide.Revision, Is.EqualTo(3));
        Assert.That(guide.IsValid(), Is.True);
    }

    private static GuideBookContent CreateGuide() => new()
    {
        Categories = new List<GuideBookCategory> { new() { Id = "cat-1", Name = "Getting Started" } },
        Pages = new List<GuideBookPage> { new() { Id = "page-1", CategoryId = "cat-1", Title = "Welcome", Body = "Hello" } },
    };
}
