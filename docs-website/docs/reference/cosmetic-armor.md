# Skins and Skin Shop

Open inventory and use the header arrows to switch between **Gear** and **Skin**.
The Skin page has a **full-set armor** slot, right weapon, left weapon/shield,
and wing/cape. Pets are equipped on Gear. Right-hand weapon skins can also be placed in the left-hand
slot, including while another weapon skin is in the right hand. Class restrictions
still apply. The bag stays visible on both pages.

One armor skin changes the appearance of the helmet, armor, pants, gloves and
boots together. Wings, weapons, shields and pets remain separate items. Real
combat equipment stays equipped underneath the skins. Set 315 is restricted to
Elf and Summoner, including their promotions; Set 100 is unrestricted.
Nearby players see the equipped skin's upgrade level on every piece of a full
set, including its +15 appearance, both when entering view and when skins change.
For imported full sets, deploy the updated server and apply **Set 315 Cosmetic Armor
version 16** with all players offline, then restart and reconnect both clients. This
adds the missing visual pieces so nearby players see the same full set as its owner.
Sets without a helmet keep the underlying helmet; only the armor is a usable skin item.

Select an owned skin and choose **Use**, or drag it onto its matching Skin slot.
Tap an occupied Skin slot to return the item to the bag; make room first.
Unequip skins before trading, warehousing or listing them in a personal store.
Skins are permanent and tradable. Players cannot drop them or sell them to NPCs.

## Mount and pet slots

Gear has one large slot for riding helpers (Uniria, Dinorant, Dark Horse,
Fenrir and custom mounts) and one small slot beside it for ordinary pets
(Guardian Angel, Imp, Guardian, Panda and other non-mount followers).
A mount and a pet can be equipped together. Ordinary pets keep their usual
bonuses and durability rules. Mounts cannot use the small slot; ordinary pets
cannot use the large slot. The Skin page has no pet slot. Muun equipment is
unchanged.

Use a bag pet or drag it onto the small Gear slot. Tap an equipped pet to return
it to the bag; make room first. Install the matching client and apply **Set 315
Cosmetic Armor version 14** with all players offline, then restart. Existing
ordinary pets in the large slot move to the small slot, or into the bag when
it is occupied. If the bag cannot fit them, the update stops without removing
owned items; free space and retry.

## Rank titles

Open **Rank Shop** from the window menu or the modern theme's **Rank** button.
The **Shop** tab offers fourteen titles for WCoinC. VIP (rank 1), PvP King
(rank 7), and Destroyer (rank 9) are earned and appear in **My Ranks** only while
eligible. Rank 10 is retired. Other titles initially cost **100 WCoinC** each.
Purchases apply pending coin grants and save the payment and permanent
character ownership together.

VIP follows account VIP status. Activating a Gold Channel Ticket grants VIP
for the product's configured duration; additional tickets extend the remaining
time. An administrator or website integration can set the account's
`VipExpiresAtUtc` in UTC or use the existing `IsVip` account attribute.
PvP King belongs to the player with the most completed duel wins. Cancelled
duels do not add wins. Destroyer belongs to the current week's leading player
killer. Weeks begin Monday at midnight in the server's local time zone. Ties go
to whoever reached the score first. Standings include offline players and survive
restarts. Other game servers synchronize within five seconds.
Duel wins start accumulating after deployment; older duel results were not stored.

**Equip** shows a selected eligible title. Earned titles appear automatically
when no eligible title is selected, with Destroyer taking priority over PvP King
and VIP. **Unequip** hides all titles until another is equipped. Titles grant no
combat bonuses. Static badges are 10% smaller, while GIF badges display at
80 by 20 units with a two-unit upward offset in both clients.

Player deaths caused by another player also send mailbox mail naming the killer.
Self deaths and deaths to monsters do not send this mail or count toward
Destroyer. Repeated player kills, including duel kills, count toward the weekly
standings. The PK penalty remains separate from these statistics.

Deploy the updated server and apply the **RankTitles** database schema migration
and **EarnedRankStandingsAndEquipmentDrops** through the normal server database
update flow. Gold Ticket purchases also require **Add cash shop** version 7. Existing Season 6 servers must
also apply **TwinMU Rank Titles** version 4 in **Updates**, then restart the
game server. The update installs the current titles and retires TwinMU III while preserving custom prices
and the active state of existing configurations.
Fresh configurations include its default settings with the feature disabled,
so enable **Rank Titles** before using the shop. Edit each
`Titles` entry's `Price` in the **Rank Titles** feature plugin configuration.
A zero price prevents purchases while keeping owned titles available to equip.
Keep `Rank` IDs aligned with the desktop and mobile `RankTitles.json` files.
Open **Game Configuration → Rank Title Prices** to edit prices. Expand
**Titles**, change each title's **WCoinC price**, and submit the form.
A zero price disables new purchases. Players can refresh the Rank Shop to
see the saved prices. The badge appearance, animated effects, and display
size are configured in the desktop and mobile `RankTitles.json` catalogs.

## Buying and previewing

On the Skin page, tap **Skin Shop**. The shop shows eight items in four columns,
with your character preview on the right. Each item has its own Buy button below
the preview and name. Buy opens a bonus selection popup; select a bonus and
confirm the purchase there, or Cancel to leave without buying. Item cards and
the large preview show weapons, wings, pets, and mounts with your character.
Drag left or right to rotate the large preview; on mobile, swipe.

The Weapons category has a type filter for swords, maces, spears, bows,
staffs/sticks, scepters, books and RF gloves. Weapons that your class cannot
equip are hidden from the item list. The server checks these restrictions
again when buying or equipping.

The shop opens above the inventory and HUD. Confirmations and tooltips remain
above the shop. Install the matching client UI assets with each client update;
the shop's shared stylesheet provides its three-column layout in both themes.

All categories initially cost **100 WCoinC** each. In the server admin panel,
open the **Skin Shop** feature plugin configuration to edit `ArmorPrice`,
`WeaponPrice`, `ShieldPrice`, `WingPrice`, `PetPrice`, and `MountPrice` separately. A zero price
allows preview but disables purchases for that category. Disabling the plugin
closes the shop. Prices and bonus choices come from the server. A changed price,
full bag, invalid bonus or insufficient balance cannot spend WCoinC.

## Standalone mounts and animation

The Mounts category contains 13 imported Fenrir models. These are actual riding
items, equipped in the **Gear helper** slot, with the normal Fenrir level/class
requirements, speed, Plasma Storm skill and durability. They occupy 2x2 bag cells.
They have no selectable skin bonus. An ordinary or cosmetic pet can remain in the small Gear pet slot while riding. Previewing a mount also previews the rider in the shop window.

Apply **Set 315 Cosmetic Armor version 8** while players are offline and restart.
Existing floating Fenrir items keep their identities and become mounts; items in
the Skin pet slot return to the bag. Free bag space if the update reports it cannot
fit. Existing real helper equipment is not replaced.

Custom wings and followers use forward, interpolated playback independent of
render FPS. Wings use their own BMD key count and the reference idle/flight speeds;
changing speed preserves the pose, while stun and sleep pause it.
Mount actions follow the normal Fenrir riding actions; models missing a skill or
damage action use their attack or standing pose. Imported light and mesh-glow
settings remain active. Wing204 and older wing180 and wing213 have single-key BMDs and stay static; it needs
a replacement animated model to flap. DLL-only source effects are unavailable.

Walking pets follow beside the character at ground height rather than orbiting
in the air. Flying/magical followers retain their configured hover animation.
The preview uses the same model height and displays ground pets standing.

## Bonuses

A chosen excellent option changes the skin bonus only. It does not add an
Excellent name prefix, a green name, or excellent mesh tint. Normal model glow
and imported effects remain unchanged.

A full-set armor skin has **one standard excellent defense option**, applied
once for the whole set. Weapon skins offer the normal physical or wizardry
excellent options for their type. Shields use normal excellent defense options.
Wing and cape skins offer the normal third-wing options: 5% ignore defense,
5% complete damage reflection, 5% full HP recovery, or 5% full MP recovery.
These are the server's existing item options and normal values; no custom
percentage bonuses are invented. Administrators can edit those option
definitions in the normal game configuration.

Each non-pet skin carries one selected option. Its bonus applies only while
validly equipped in Skin, in addition to the real gear's attributes. It stops
applying when unequipped and returns after relogging with the skin equipped.
The skin grants no base weapon damage, armor defense, wing absorption, flight,
movement benefit or ordinary gear set-completion bonus. Pet skins remain
cosmetic followers without combat bonuses, attacks, mounts or item collection.
Normal jewels and gear upgrades are not a skin-bonus reroll system.

## Existing items and server deployment

Deploy the matching OpenMU source, take all players offline, apply **Set 315
Cosmetic Armor version 8** in the admin panel, and restart. Use the matching
client data and rebuild your client. No APK is built by this change.

Every old Set 100/315 helmet, pants, gloves or boots skin becomes a separate
copy of its corresponding full-set armor item. Existing armor skins become
full-set items too. Item identities and ownership are preserved: five old
pieces become five full-set items. The armor already in Skin stays there;
retired body-slot items return to the bag. Old skins without an option receive
the first configured normal bonus for their type; pets keep no bonus.

The update preflights all bag, extension, personal-store and warehouse pages
before committing positions. Larger full-set items can require more space.
If a page cannot fit, the error names the character/account and page. Move
items to another page using the previous version, then retry. The failed
conversion restores definitions, options and positions; no owned items are
deleted. Successful repacking may rearrange items within that page. It does not
transfer items between pages, characters or accounts.

The previous SQL five-piece installer is retired. Use the shop or GM commands.
GM examples: `/item 8 100` or `/item 8 315` creates a full-set item with its
default bonus; `/item 8 100 0 8` selects the normal damage-decrease excellent
flag. The usual excellent flag is the fourth argument: choose exactly one
of 1, 2, 4, 8, 16, 32 where that item's pool supports it. Wings use 1, 2, 4, 8.
Omit skill, luck, normal-option and ancient arguments for skins. Pets accept no
bonus. Retired body-piece commands are rejected with a full-set command hint.

Version 8 adds `/item 2 101` (Battle Scepter Skin, DL), `/item 5 101`
(Summoner Book Skin), and `/item 0 101` (Sacred Glove Skin, RF). These three
variants reuse existing Season 6 models because matching readable custom models
were unavailable. The supplied custom bow, staff and summoner sticks remain in
the Weapons list for their eligible classes. Season 6 books use the normal
excellent wizardry options.

Version 8 also applies weapon class limits: swords/spears for DK and MG,
maces for DK/MG/DL/RF, bows for Elf, wizard staffs for WZ/MG, summoner sticks
and books for Summoner, scepters for DL and gloves for RF, including promotions.
Incompatible equipped skins return to the bag without losing their identity or
bonus. Make space before updating if the conversion cannot fit. Apply offline
and restart the server.

Other original commands remain: sword `/item 0 100`, mace `/item 2 100`, bow
`/item 4 100`, staff `/item 5 100`, shield `/item 6 100`, capes `/item 12 180`
and `/item 12 213`, wings `/item 12 200` and `/item 12 210`, pet `/item 13 200`.
The [partial FinalMU import](finalmu-skins.md) lists its additional item IDs.

Models and textures live under `Data/Custom/Skins`. The first pack includes
Set100, Set315, SWORD, MACE, BOW, STAFF, SHIELD, WING and CAPA. The second pack
adds Wing210, wing213 and Pet208. Its other gear has not been imported.
The original effect files are retained there. Static sprites and pet mesh glow
are stored with their model definitions under `Data/Items/Models`.

The supplied combined first-pack effects include sword 101 without a matching
model, and some mace bone indices beyond the supplied model's bone count.
Those rows are retained in the original file; unmatched items are not invented
and invalid bone accesses are skipped. The second pack's combined effect file
reuses IDs from its other gear, so its wing-specific files are used for wing
210 and cape 213. Pet208 uses its supplied mesh-glow colors and flags.

Server transformations and Chaos Castle appearances take precedence. Check fit,
animation, effects and mobile performance in game before distributing the
client. Cosmetic pets face their travel direction; cosmetic wings use the
normal idle and flight frame rates. Run `python tools/test_cosmetic_armor.py`
for asset checks and the inventory/shop layout scripts under `android/` for
native RmlUi checks.


### Character combat statistics

The custom mobile client's character window has an Attributes / Statistics switch.
The statistics response uses live server attributes, including equipped skin
bonuses. Chances and multiplier rows use percentage points: a configured 0.10
excellent-damage chance is displayed as 10%. This is a chance to trigger excellent
damage, not a 10% addition to the ordinary damage range. The page refreshes once
per second while visible. Both the updated server and client are required; no
configuration or database migration is needed.

The additional reference wing skins are `rf5` (`/item 12 382`), `sum7lvl`
(`/item 12 383`) and `rf7lvl` (`/item 12 384`). Their verified bone effects
are included in the matching client data. Publish the server and apply
**Set 315 Cosmetic Armor version 10** with all players offline before creating
or selling them. Existing wings need only the rebuilt client and packaged data
for the animation, 25 fps atlas and reference lighting fixes.

The shop combines local and server class requirements, including promotion
limits. All-class placeholder sets use the reviewed male or female model
family; specific class requirements from the reference are retained. Apply
**Set 315 Cosmetic Armor version 20** for matching server restrictions. Owned
items remain accessible in Skin Inventory for removal or trade. Armor 69 (8:394)
is restricted to High Elf and Dimension Master. The complete model review also
corrects female Armor 66, 67, 68, 71 and 80, and male sets that were previously
labelled female. Restart the client after installing the updated item data. The DK, DL, DW, Elf, MG,
Summoner and Rage Fighter wing variants use their matching class; incompatible
skins do not appear on your character or in its preview.

## Equipment drop options

Apply **380 and socket equipment drop options** to an existing Season 6 database.
Newly initialized servers already include it. Level-380 equipment (identified by
its Guardian option eligibility) and socket equipment roll luck once at 30%.
Newly generated socket drops, including boxes, have at most two socket slots.
The item definition retains its crafting capacity, and existing items are unchanged.


## Skin shop availability and individual prices

Open **Game Configuration > Skin Shop** in the administration panel. Each category
has an enable-sales checkbox. Each skin has its own **Sell** checkbox and WCoinC
price. To sell only Immortal WareWolf Armor, enable Sets, uncheck the other set
rows, and set its price. Save changes and have players refresh their shop.
Unchecked items and disabled categories remain preview only; owned skins stay
available for equipping, removal and trading. A category default price of zero
also disables all sales in that category. Defaults fill unconfigured entries;
existing individual prices are retained. A stale quoted price or disabled sale
is rejected before spending coins. Restart game servers if live plugin reload
is unavailable in your deployment.

The character preview's +0–+15 controls are visual only. A separate +0–+15 selector
in the purchase popup sets the bought skin's level, including wings. It starts at
+0 whenever the popup opens.

Apply **Set 315 Cosmetic Armor version 20** with all players offline for female
Armor 72 (8:397), Armor 74 (8:399) and Armor 77 (8:402), restricted to High Elf and
Dimension Master. The update preserves existing shop settings and returns
incompatible equipped skins to storage as in earlier versions.
