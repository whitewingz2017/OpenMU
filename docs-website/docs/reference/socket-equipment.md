# Season 6 socket equipment

These are the socket items configured by the Season 6 initializer. Names refer
to the server configuration; some client translations use different names.
Character promotions keep their class family's equipment restrictions.

- **Dark Knight / Blade Knight / Blade Master:** Titan (45), Brave (46).
  Weapons: Flamberge (0:26), Sword Breaker (0:27). Shield: Crimson Glory (6:17).
- **Dark Wizard / Soul Master / Grand Master:** Hades (52).
  Weapon: Imperial Staff (5:31). Shield: Guardian Shield (6:20).
- **Fairy Elf / Muse Elf / High Elf:** Seraphim (49), Faith (50).
  Weapons: Stinger Bow (4:23), Frost Mace (2:16). Shield: Frost Barrier (6:19).
- **Magic Gladiator / Duel Master:** Destory (47; spelling in the server),
  Phantom (48). Neither set has a helmet.
  Weapons: Imperial Sword (0:28), Deadly Staff (5:30).
  Shield: Salamander Shield (6:18).
- **Dark Lord / Lord Emperor:** Paewang (51), also called Royal by the client.
  Weapon: Absolute Scepter (2:17).
- **Summoner / Bloody Summoner / Dimension Master:** Eternal Wing Stick (5:20).
  Queen (53) is mentioned by the socket initializer, but its armor item
  definitions are missing, so it is not an available socket set in this checkout.
- **Rage Fighter / Fist Master:** Tigris (146), with Pantera Claws (0:52).
  Tigris has a helmet, armor, pants and boots; Rage Fighter uses claw weapons
  instead of an armor glove piece.

An armor set number applies to groups 7 (helm), 8 (armor), 9 (pants), 10 (gloves)
and 11 (boots), except where a piece is absent. Weapon IDs above use group:number.
All listed armor, shields and weapons support three sockets, except the Stinger
Bow, which supports five. Socket capacity does not guarantee that a newly created
item has empty sockets: configure the socket count when creating or dropping it.

## Adding Tigris to an existing server

Install the updated server and matching PC/mobile item catalogs and assets.
With players offline, apply **Tigris Socket Equipment** in the admin panel's
configuration updates, then restart the game server. Fresh Season 6
configurations already include the five new items.

Search the item list for **Tigris Helm**, **Tigris Armor**, **Tigris Pants**,
**Tigris Boots**, and **Pantera Claws**. They are ordinary combat gear, worn on
the Gear page. Each supports three sockets and +0 through +15. They start with
the existing Phoenix Soul base stats, level 380 requirements, Rage Fighter/Fist
Master restrictions and normal enhancement/set bonuses. Tigris armor uses its
own four-piece set bonuses; it does not complete a Phoenix Soul set. Pantera
Claws can be worn in either hand and count as glove weapons for Rage Fighter skills.

The update preserves existing definitions and owner stat edits on repeat runs.
If one of its item IDs is already used by unrelated custom equipment, it stops
instead of replacing that equipment. Existing owned items are not converted.
