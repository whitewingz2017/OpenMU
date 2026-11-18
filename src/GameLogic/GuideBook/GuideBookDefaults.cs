// <copyright file="GuideBookDefaults.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

/// <summary>
/// The guide which a new server starts with: the server's own features, as the client of this server
/// shows them. Game masters change it in the game afterwards.
/// </summary>
/// <remarks>
/// The pages quote the live settings through the keys of <see cref="GuideBookValues"/>, e.g. "{exc.base}",
/// so they stay right when an administrator changes a rate.
/// </remarks>
internal static class GuideBookDefaults
{
    private const string SystemsCategory = "cat-systems";

    private const string EventsCategory = "cat-events";

    /// <summary>Creates the default guide.</summary>
    /// <returns>The default guide.</returns>
    public static GuideBookContent Create() => new()
    {
        Categories = new List<GuideBookCategory>
        {
            new() { Id = "cat-start", Name = "Getting Started" },
            new() { Id = "cat-systems", Name = "Game Systems" },
            new() { Id = "cat-rewards", Name = "Rewards" },
            new() { Id = "cat-events", Name = "Events" },
        },
        Pages = new List<GuideBookPage>
        {
            new()
            {
                Id = "page-welcome",
                CategoryId = "cat-start",
                Title = "Welcome, Adventurer",
                Body = "![Interface/RmlUi/assets/guide/welcome.tga]\n"
                    + "Welcome to the Guide Book. Here you find how this server's systems, rewards and events work.\n"
                    + "\n"
                    + "## Finding your way\n"
                    + "- Open the book with **F1** or the glowing **Guide Book** left of the daily reward.\n"
                    + "- Pick a category on the left, then a page.\n"
                    + "- Type in **Search** to find every page that mentions a word.\n"
                    + "- Gold underlined words open another page, like [[page-excellent|Excellent Combination]].\n"
                    + "\n"
                    + "> The **Controls** category lists every hotkey and chat command of the game.",
            },
            new()
            {
                Id = "page-menu",
                CategoryId = "cat-start",
                Title = "Where to Find Things",
                Body = "Most of the server's own windows open from the **window menu** (the menu button on the bottom bar).\n"
                    + "\n"
                    + "- **Excellent Combination**: move an Excellent option between two items. See [[page-excellent|Excellent Combination]].\n"
                    + "- **Event Timers**: when the next Blood Castle, Devil Square and Chaos Castle open.\n"
                    + "- **Daily Rewards**: the month's login calendar. See [[page-daily|Daily Rewards]].\n"
                    + "- **Muun Inventory**, **Jewel Bank** and **Rank Shop**.\n"
                    + "\n"
                    + "> While today's daily reward is unclaimed, its icon sits above the mana gauge: click it to claim.",
            },
            new()
            {
                Id = "page-excellent",
                CategoryId = "cat-systems",
                Title = "Excellent Combination",
                Body = "![Interface/RmlUi/assets/guide/excellent_combination.tga]\n"
                    + "Move one **Excellent option** from a second item onto your main gear.\n"
                    + "\n"
                    + "## How to combine\n"
                    + "1. Open **Menu > Excellent Combination**.\n"
                    + "2. Drag your **Main Gear** and the **Secondary Gear** from your bag into the two cards. Nothing moves until you press Stack.\n"
                    + "3. Optionally add [[page-core|Resonance Cores]] to raise the success rate.\n"
                    + "4. Press **Stack**.\n"
                    + "\n"
                    + "## Rates and cost\n"
                    + "- Base success rate: **{exc.base}%**\n"
                    + "- Each Resonance Core: **+{exc.bonus}%**, up to **{exc.max}%**\n"
                    + "- Cost per attempt: **{exc.zen} Zen**\n"
                    + "\n"
                    + "## Rules\n"
                    + "- Both items must be the same equipment type and different items in your bag.\n"
                    + "- Main Gear needs one or two Excellent options; Secondary Gear needs exactly one option that Main Gear does not have.\n"
                    + "- Ancient, socket, timed and listed items cannot be combined.\n"
                    + "- With two options already, success replaces one of them at random.\n"
                    + "\n"
                    + "> Success and failure both use up the Secondary Gear, the cores and the Zen. **A failure never changes your Main Gear.**",
            },
            new()
            {
                Id = "page-core",
                CategoryId = "cat-systems",
                Title = "Resonance Core",
                Body = "{{item 14 252}}\n"
                    + "The **Resonance Core** is a violet crystal that raises the success rate of an [[page-excellent|Excellent Combination]].\n"
                    + "\n"
                    + "## Where to get it\n"
                    + "- **Bosses**: a boss can drop a core when it dies, with a **{core.boss}%** chance, but each boss drops at most one core a day.\n"
                    + "- **Any other monster**: a very rare drop, **{core.monster}%** per kill.\n"
                    + "- The core drops at the monster's feet for the player who landed the **killing blow** and their party.\n"
                    + "\n"
                    + "{{boss-rewards}}\n"
                    + "\n"
                    + "## Using cores\n"
                    + "- Tap the core icon in the Excellent Combination window and choose how many to use.\n"
                    + "- Each core adds **{exc.bonus}%** to the base **{exc.base}%**, up to **{exc.max}%**.\n"
                    + "- Only cores in your normal bag count. Cores stack up to 255 in one slot.\n"
                    + "\n"
                    + "> Cores are used up whether the combination succeeds or fails.",
            },
            new()
            {
                Id = "page-skins",
                CategoryId = "cat-systems",
                Title = "Skin Shop & Mounts",
                Body = "The **Skin Shop** sells armor, weapon, wing, pet and mount skins.\n"
                    + "\n"
                    + "- Press **Buy**, choose the purchase level from +0 to +15 and a bonus, then confirm.\n"
                    + "- Bonuses only raise HP, mana or Zen: armor and shields give max HP, max mana or Zen; weapons give HP or mana after a kill; wings give full HP or mana recovery.\n"
                    + "- Drag the character in the preview to turn it.\n"
                    + "\n"
                    + "## Skin Inventory\n"
                    + "Drag a skin from your bag into the **Skin Inventory** to store it, or choose **Remove** to take it back.\n"
                    + "\n"
                    + "> Mounts are open to every class from its first class on. Class wings appear only for their class.",
            },
            new()
            {
                Id = "page-helper",
                CategoryId = "cat-systems",
                Title = "MU Helper",
                Body = "The **MU Helper** hunts for you around the spot where you start it.\n"
                    + "\n"
                    + "1. Press the gear beside the map position at the top left to open its settings.\n"
                    + "2. Choose a primary attack, and optionally more attacks, buffs and potions.\n"
                    + "3. Press the start button beside it, or the **Home** key.\n"
                    + "\n"
                    + "- Without Combo, extra attacks without Delay or Condition take turns with the primary attack.\n"
                    + "- Skills you have not learned or that are cooling down are skipped.\n"
                    + "- A healing potion is drunk first whenever your HP is below the threshold.",
            },
            new()
            {
                Id = "page-bosses",
                CategoryId = "cat-rewards",
                Title = "Boss Bounties",
                Body = "![Interface/RmlUi/assets/guide/boss_rewards.tga]\n"
                    + "Bosses carry a bounty of **WCoin** and **Goblin Points**, and they can drop a [[page-core|Resonance Core]].\n"
                    + "\n"
                    + "{{boss-rewards}}\n"
                    + "\n"
                    + "## Who gets it\n"
                    + "- The coins go only to the player who lands the **killing blow**. Party members and other attackers get none of them.\n"
                    + "- The coins are added to your account at once, and the server announces who killed the boss.\n"
                    + "- A core drops at the boss's feet for the killer and their party, at most once per boss a day.\n"
                    + "\n"
                    + "> Coins go to the account, so every character of that account can spend them in the cash shop.",
            },
            new()
            {
                Id = "page-daily",
                CategoryId = "cat-rewards",
                Title = "Daily Rewards",
                Body = "Log in every day and claim the day's reward in the **Daily Rewards** window.\n"
                    + "\n"
                    + "- The window opens by itself when you enter the game while today's reward is unclaimed.\n"
                    + "- Open it any time from the window menu or the icon above the mana gauge.\n"
                    + "- Only today can be claimed. A day that passes unclaimed is lost, and the calendar starts over every month.\n"
                    + "- One claim per day for the whole account.\n"
                    + "- Days follow the **server's** time.\n"
                    + "\n"
                    + "> If the item does not fit in your bag, nothing is granted and the day stays claimable.",
            },
            new()
            {
                Id = "page-coins",
                CategoryId = "cat-rewards",
                Title = "WCoin & Goblin Points",
                Body = "![Interface/RmlUi/assets/guide/wcoin.tga]\n"
                    + "**WCoinC**, **WCoinP** and **Goblin Points** are your account's cash shop currencies.\n"
                    + "\n"
                    + "## How to earn them\n"
                    + "- [[page-bosses|Boss Bounties]]: the boss's killer earns them.\n"
                    + "- [[page-daily|Daily Rewards]]: some days pay coins.\n"
                    + "\n"
                    + "## How to spend them\n"
                    + "Open the cash shop and choose the currency to pay with.\n"
                    + "\n"
                    + "![Interface/RmlUi/assets/guide/goblin_points.tga]",
            },
            new()
            {
                Id = "page-bc",
                CategoryId = "cat-events",
                Title = "Blood Castle",
                Body = "Defeat the castle's monsters, break the gate, slay the magic skeletons and destroy the statue, then return the **Archangel's weapon** before the time runs out.\n"
                    + "\n"
                    + "- The event panel at the right edge shows the time left and every objective in order.\n"
                    + "- Finished objectives are struck through; the current one is highlighted.\n"
                    + "- Check **Menu > Event Timers** for when the next castle opens.\n"
                    + "\n"
                    + "> Bring potions and a party: the castle's monsters get stronger with its level.",
            },
            new()
            {
                Id = "page-ds",
                CategoryId = "cat-events",
                Title = "Devil Square",
                Body = "Survive the waves of monsters and score points.\n"
                    + "\n"
                    + "- The event panel shows each wave, when the next one starts, your points and your rank.\n"
                    + "- Check **Menu > Event Timers** for when Devil Square opens.",
            },
            new()
            {
                Id = "page-cc",
                CategoryId = "cat-events",
                Title = "Chaos Castle",
                Body = "The last character standing wins.\n"
                    + "\n"
                    + "- The event panel counts the characters remaining.\n"
                    + "- When the match begins every window closes and the chat stays closed until it ends.\n"
                    + "- Check **Menu > Event Timers** for when Chaos Castle opens.",
            },
        }.Concat(LaterPages()).ToList(),
    };

    /// <summary>
    /// Adds the pages which came after the first default guide to a guide made before them, once: a page which a
    /// game master deletes afterwards stays deleted. The change is saved with the guide's next save.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public static void AddLaterPages(GuideBookSettings settings)
    {
        lock (settings)
        {
            var added = false;
            foreach (var page in LaterPages())
            {
                if (settings.AddedDefaultPages.Contains(page.Id))
                {
                    continue;
                }

                settings.AddedDefaultPages.Add(page.Id);
                added |= Add(settings.Published, page) | Add(settings.Draft, page);
            }

            if (added)
            {
                // A new revision, so clients show the pages; the draft stays published if it was.
                var unpublished = settings.HasUnpublishedChanges();
                settings.Published.Revision++;
                settings.Draft.Revision = unpublished ? settings.Draft.Revision + 1 : settings.Published.Revision;
            }
        }
    }

    private static bool Add(GuideBookContent content, GuideBookPage page)
    {
        if (content.Pages.Any(existing => existing.Id == page.Id) || content.Pages.Count >= GuideBookContent.MaximumPages)
        {
            return false;
        }

        if (content.Categories.All(category => category.Id != page.CategoryId))
        {
            if (content.Categories.Count >= GuideBookContent.MaximumCategories)
            {
                return false;
            }

            content.Categories.Add(new GuideBookCategory { Id = page.CategoryId, Name = page.CategoryId == EventsCategory ? "Events" : "Game Systems" });
        }

        var added = new GuideBookPage { Id = page.Id, CategoryId = page.CategoryId, Title = page.Title, Body = page.Body };
        content.Pages.Add(added);
        if (content.IsValid())
        {
            return true;
        }

        // The guide is full: the client would refuse it with the page.
        content.Pages.Remove(added);
        return false;
    }

    // The pages of later versions of the default guide, in their categories' order.
    private static IEnumerable<GuideBookPage> LaterPages()
    {
        yield return OfflineLevelingPage();
        yield return ItemAttributesPage();
        yield return PersonalStorePage();
        yield return InvasionPage("page-golden", "Golden Invasion", "{invasion.golden}", "Golden monsters invade the cities and fields. Golden monsters are stronger than their common kin; hunt them before the invasion ends.");
        yield return InvasionPage("page-red-dragon", "Red Dragon Invasion", "{invasion.reddragon}", "Red Dragons attack one of the cities' fields.");
        yield return InvasionPage("page-white-wizard", "White Wizard Invasion", "{invasion.whitewizard}", "The White Wizard leads his ogre corps into one map. The server announces where he falls.");
        yield return KanturuPage();
        yield return KalimaPage();
        yield return DoppelgangerPage();
        yield return ImperialGuardianPage();
        yield return CastleSiegePage();
        yield return LandOfTrialsPage();
        yield return CrywolfPage();
    }

    private static GuideBookPage CastleSiegePage() => new()
    {
        Id = "page-castle-siege",
        CategoryId = EventsCategory,
        Title = "Castle Siege",
        Body = "Guilds fight for **Loren Castle** in the Valley of Loren. The guild which holds it rules the castle: it sets the castle's taxes and opens the [[page-land-of-trials|Land of Trials]].\n"
            + "\n"
            + "## Taking part\n"
            + "1. During the registration, your guild master registers the guild with the **Guardsman** at the castle bridge in the Valley of Loren.\n"
            + "2. Then the guild hands in **Signs of Lord** there. The guilds with the most signs are chosen to attack.\n"
            + "3. On the day of the siege, fight your way into the castle.\n"
            + "\n"
            + "## Winning\n"
            + "- Your guild master takes the **Crown** while your side holds both **Crown Switches**: the castle is yours.\n"
            + "- If no attacker does before the siege ends, the defending guild keeps the castle.\n"
            + "\n"
            + "> The server announces each period: the registration, the handing in of the signs and the siege.",
    };

    private static GuideBookPage LandOfTrialsPage() => new()
    {
        Id = "page-land-of-trials",
        CategoryId = EventsCategory,
        Title = "Land of Trials",
        Body = "A hunting ground behind Loren Castle, open while a guild holds the castle.\n"
            + "\n"
            + "## How to enter\n"
            + "1. Go to the Valley of Loren and talk to the **guard at the Land of Trials entrance**.\n"
            + "2. The castle guild's alliance enters free; other players pay the entry fee in Zen.\n"
            + "\n"
            + "- The castle's guild master sets the fee, and can close the Land of Trials.\n"
            + "\n"
            + "> While no guild holds the castle, the Land of Trials stays closed. See [[page-castle-siege|Castle Siege]].",
    };

    private static GuideBookPage CrywolfPage() => new()
    {
        Id = "page-crywolf",
        CategoryId = EventsCategory,
        Title = "Crywolf Fortress",
        Body = "Defend **Crywolf Fortress** against the army of **Balgass**.\n"
            + "\n"
            + "## How it works\n"
            + "- When the event starts, the army attacks the fortress.\n"
            + "- **Elves** stand at one of the five **altars** and stay there to contract it. The contracts protect the **Statue of the Holy Wolf**. Moving, dying or leaving cancels a contract, and each altar takes only a few per event.\n"
            + "- Everyone else fights the army and guards the elves at the altars. At the end, **Balgass** himself attacks.\n"
            + "- The players with the highest scores become the heroes of the event.\n"
            + "\n"
            + "## The result, until the next event\n"
            + "- **Defended**: monsters have less HP, and the chaos machine mixes of event tickets succeed more often.\n"
            + "- **Occupied**: jewels drop less often, and monsters give less experience.\n"
            + "\n"
            + "> The server announces when Crywolf starts.",
    };

    private static GuideBookPage PersonalStorePage() => new()
    {
        Id = "page-personal-store",
        CategoryId = SystemsCategory,
        Title = "Personal Store",
        Body = "Sell your items to other players from your own store, while you stand in the world.\n"
            + "\n"
            + "## Opening your store\n"
            + "1. Open your inventory (**I** or **V**) and press the **Personal Store** button at its bottom.\n"
            + "2. Drop an item into the store, or right-click one in it, and enter its price.\n"
            + "3. Name your store and open it. Your store's name shows above your character.\n"
            + "\n"
            + "## Prices\n"
            + "- Every item is sold for **Zen**, or, when the server allows it, for **WCoinC**, **WCoinP**, **Goblin Points** or **jewels** (Bless, Soul, Chaos, Creation, Life and more).\n"
            + "- The price dialog's **Currency** button picks the currency; other items the server allows are behind **Other items**.\n"
            + "\n"
            + "## Buying\n"
            + "- Click the store's name above a player to look inside.\n"
            + "- The tooltip shows each price with its currency, e.g. **Selling Price: 12 Jewel of Bless**.\n"
            + "- Jewel prices are paid with the jewels of your inventory; a jewel bundle counts as its number of jewels. There is no change, so carry jewels which match the price exactly.\n"
            + "\n"
            + "> See also the [[page-coins|WCoin & Goblin Points]] you can sell for.",
    };

    // The monsters, their maps and how often the invasion comes are the live settings: GuideBookLivePages.Expand writes them in.
    private static GuideBookPage InvasionPage(string id, string title, string token, string introduction) => new()
    {
        Id = id,
        CategoryId = EventsCategory,
        Title = title,
        Body = introduction + "\n"
            + "\n"
            + token + "\n"
            + "\n"
            + "> Check **Menu > Event Timers** for the next invasion. The bosses among them pay the rewards of [[page-bosses|Boss Bounties]].",
    };

    private static GuideBookPage KanturuPage() => new()
    {
        Id = "page-kanturu",
        CategoryId = EventsCategory,
        Title = "Kanturu: Maya & Nightmare",
        Body = "Fight your way through the Refinery Tower of Kanturu to its master, the **Nightmare**.\n"
            + "\n"
            + "## How to enter\n"
            + "1. Wear a **Moonstone Pendant**.\n"
            + "2. Go to **Kanturu Relics** and talk to the **Gateway Machine** while the event is open.\n"
            + "\n"
            + "## The battle\n"
            + "1. **Maya** rises: defeat her hands and her guards before the time runs out.\n"
            + "2. The **Nightmare** appears: defeat him to break the barrier.\n"
            + "3. The **Refinery Tower** opens for a while to everyone who wins.\n"
            + "\n"
            + "> Check **Menu > Event Timers** for when the gateway opens.",
    };

    private static GuideBookPage KalimaPage() => new()
    {
        Id = "page-kalima",
        CategoryId = EventsCategory,
        Title = "Kalima & Kundun",
        Body = "Deep in **Kalima** waits **Kundun**, guarded by the Kalima monsters.\n"
            + "\n"
            + "## How to enter\n"
            + "1. Collect **Symbols of Kundun** from monsters.\n"
            + "2. Fill a stack of the same level: it becomes a **Lost Map**.\n"
            + "3. Drop the Lost Map to open a gate to the Kalima of its level, for you and your party.\n"
            + "\n"
            + "- The higher the map's level, the stronger Kalima and its Kundun.\n"
            + "- Kundun is one of the bosses of [[page-bosses|Boss Bounties]].",
    };

    private static GuideBookPage DoppelgangerPage() => new()
    {
        Id = "page-doppelganger",
        CategoryId = EventsCategory,
        Title = "Doppelganger",
        Body = "Stop the monsters and your own doppelgangers before they reach the end of their path.\n"
            + "\n"
            + "## How to enter\n"
            + "1. Collect **Signs of Dimensions** and fill a stack: it becomes a **Mirror of Dimensions**.\n"
            + "2. Talk to **Lugard** in Elbeland with the mirror in your inventory, alone or with your party.\n"
            + "\n"
            + "## The battle\n"
            + "- Monsters walk along the path; don't let them pass.\n"
            + "- Defeat the bosses for their reward chests.",
    };

    private static GuideBookPage ImperialGuardianPage() => new()
    {
        Id = "page-imperial",
        CategoryId = EventsCategory,
        Title = "Imperial Guardian",
        Body = "Fight through the fortress of the Imperial Guardian, gate by gate, to its commanders.\n"
            + "\n"
            + "## How to enter\n"
            + "1. Collect **Suspicious Scraps of Paper** and fill a stack: it becomes **Gaion's Order**.\n"
            + "2. Talk to **Jerint the Assistant** in Elbeland with the order in your inventory, alone or with your party.\n"
            + "\n"
            + "## The battle\n"
            + "- Break each gate and defeat its guardians before the time runs out.\n"
            + "- The fortress and its commander change with the day of the week.",
    };

    // The prices, the durations and the protection are the live settings: GuideBookLivePages.Expand writes them in.
    private static GuideBookPage OfflineLevelingPage() => new()
    {
        Id = "page-offline",
        CategoryId = SystemsCategory,
        Title = "Offline Leveling",
        Body = "Keep leveling while you are offline: your character stays in the world and the **MU Helper** keeps hunting.\n"
            + "\n"
            + "## How to start\n"
            + "1. Stand where you want to hunt and start the [[page-helper|MU Helper]].\n"
            + "2. Type **/offlevel** in the chat.\n"
            + "3. In the **Offline Leveling** window choose how to pay, how long and the protection, then press **Start**.\n"
            + "4. The game closes and your character keeps hunting. Log in again to stop it.\n"
            + "\n"
            + "## Prices\n"
            + "{offline.prices}\n"
            + "\n"
            + "## Duration\n"
            + "{offline.duration}\n"
            + "\n"
            + "## Protection\n"
            + "{offline.protection}\n"
            + "\n"
            + "> You must be alive, on a map, not in an event, and the MU Helper must be running.",
    };

    // The items and their attributes are the live settings: GuideBookLivePages.Expand writes them in.
    private static GuideBookPage ItemAttributesPage() => new()
    {
        Id = "page-item-attributes",
        CategoryId = SystemsCategory,
        Title = "Custom Item Attributes",
        Body = "Some of this server's custom items, like the custom mounts, carry extra attributes. Their tooltip shows them in **purple**.\n"
            + "\n"
            + "{items.attributes}\n"
            + "\n"
            + "## In Combat statistics\n"
            + "While you wear such an item, **Combat statistics** (Character window, **Statistics**) shows a statistic without it and, in purple, what the item adds: e.g. **Defense (PvM): 1676.57 (+250)**.\n"
            + "- Attributes which no statistic shows, like the movement speed, get a purple line of their own.",
    };
}
