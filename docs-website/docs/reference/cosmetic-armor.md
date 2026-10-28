# Cosmetic skins

Open inventory and use the header arrows to switch between **Gear** and **Skin**.
The Skin page has nine positions: five armor parts, right weapon, left weapon or
shield, wing/cape, and pet. The bag stays visible on either page.

Select an owned skin item and choose **Use**, or drag it onto its matching Skin
slot. Tap an occupied Skin slot to return the item to the bag; make room first.
Unequip skins before trading, storing them, or listing them in a personal store.
Skins are permanent, tradable, and have no combat attributes. They cannot be
sold to NPCs or dropped by players. GM creation can place them on the ground.
The cosmetic pet follows the character without adding pet combat bonuses.
Cosmetic wings do not grant flight or movement benefits.

Set 315 (the female set) is restricted to **Elf and Summoner**, including their
promotions. DK, WZ, MG, DL and RF cannot equip it. Set 100 remains unrestricted.
An incompatible skin equipped before the restriction was added stays removable
from the Skin page, but its appearance is suppressed.

After applying configuration update **Set 315 Cosmetic Armor**, version 2, and
restarting the server, use these commands on a GM character:

- Set 100 armor: `/item 7 100`, `/item 8 100`, `/item 9 100`, `/item 10 100`, `/item 11 100`.
- Set 315 armor: `/item 7 315`, `/item 8 315`, `/item 9 315`, `/item 10 315`, `/item 11 315`.
- Sword: `/item 0 100`; mace/scepter: `/item 2 100`; staff: `/item 5 100`.
- Bow: `/item 4 100`; shield: `/item 6 100` (both use the left skin slot).
- First pack cape: `/item 12 180`; first pack wing: `/item 12 200`.
- Second pack wing: `/item 12 210`; second pack cape: `/item 12 213`.
- Second pack pet (folder Pet208): `/item 13 200`.

Enter each command separately. Pick up the resulting item near your GM.
Leave level, skill, luck, excellent, ancient and option arguments at defaults.
The source filenames do not determine item IDs: the first pack's `Sword140.bmd`
and `Arco37.bmd` use group 2/number 100 and group 4/number 100, respectively.

Both the updated mobile client and matching OpenMU server are required. Deploy
the server source in `C:\OpenMU\OpenMU`, apply the version 2 configuration
update in the admin panel, and restart. The older
`deploy/sql/cosmetic-armor-set315.sql` script grants only the original five
Set 315 pieces; it does not install the complete pack. Building an APK does not
deploy server changes to the VPS.

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
client. Run `python tools/test_cosmetic_armor.py` for asset and item-data checks.

The [partial FinalMU import](finalmu-skins.md) adds 176 weapon/shield, wing and pet skins
with separate GM commands and requires configuration update version 3.
