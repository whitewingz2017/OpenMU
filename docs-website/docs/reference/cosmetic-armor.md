# Skins and Skin Shop

Open inventory and use the header arrows to switch between **Gear** and **Skin**.
The Skin page has a **full-set armor** slot, right weapon, left weapon/shield,
wing/cape, and pet. Right-hand weapon skins can also be placed in the left-hand
slot, including while another weapon skin is in the right hand. Class restrictions
still apply. The bag stays visible on both pages.

One armor skin changes the appearance of the helmet, armor, pants, gloves and
boots together. Wings, weapons, shields and pets remain separate items. Real
combat equipment stays equipped underneath the skins. Set 315 is restricted to
Elf and Summoner, including their promotions; Set 100 is unrestricted.

Select an owned skin and choose **Use**, or drag it onto its matching Skin slot.
Tap an occupied Skin slot to return the item to the bag; make room first.
Unequip skins before trading, warehousing or listing them in a personal store.
Skins are permanent and tradable. Players cannot drop them or sell them to NPCs.

## Buying and previewing

On the Skin page, tap **Skin Shop**. The window has three columns: categories
on the left, actual item models in the middle, and your character preview on
the right. The larger preview starts with your character wearing no equipment
and shows only the selected skin. Use its arrows to rotate the character, and
the **Preview +** minus/plus buttons to view enhancement levels +0 through +15.
This changes the preview only; purchased skins remain +0.
Choose one bonus from the dropdown below the preview frame, review the price,
then tap **Buy**. Close or Escape ends the preview. It never changes the character
in the game world, equips an item, or grants attributes. The purchased item and its chosen bonus arrive together
in your inventory. The bonus stays on that item when traded or stored.

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
They have no selectable skin bonus. A cosmetic pet can remain in its separate Skin
slot while riding. Previewing a mount also previews the rider in the shop window.

Apply **Set 315 Cosmetic Armor version 8** while players are offline and restart.
Existing floating Fenrir items keep their identities and become mounts; items in
the Skin pet slot return to the bag. Free bag space if the update reports it cannot
fit. Existing real helper equipment is not replaced.

Custom wings and followers use forward, interpolated playback independent of
render FPS. Wing cycle length is normalized across different BMD key counts.
Mount actions follow the normal Fenrir riding actions; models missing a skill or
damage action use their attack or standing pose. Imported light and mesh-glow
settings remain active. Wing204 has a single-key BMD and stays static; it needs
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
