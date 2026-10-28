# Cosmetic armor

Open inventory and use the left/right arrows in its header to switch between
**Gear** and **Skin**. The bag stays visible on both pages. Gear shows actual
equipment; Skin shows the five cosmetic armor slots.

Set 315 has five permanent, tradable items: helmet, armor, pants, gloves and
boots. Each occupies one bag cell. On mobile, select a piece and choose **Use**
to equip it in its matching Skin slot. You can also drag a piece onto that slot.
Tap an occupied Skin slot to return its item to the bag; make room first. Unequip
a piece before trading it, putting it in the warehouse, or selling it in a
personal store. Skins cannot be dropped or sold to NPCs.

After installing the item definitions and restarting the server, a Game Master
can create each piece with the existing `/item` command:

```text
/item 7 315
/item 8 315
/item 9 315
/item 10 315
/item 11 315
```

These create the helmet, armor, pants, gloves and boots, respectively, on the
ground near the GM. Pick them up, then equip them on the Skin page. Leave the
optional level, luck, skill, excellent, ancient and option arguments at their
defaults; these are cosmetic items without combat options. Player ground drops
remain disabled; GM creation uses a separate path.

Skins change appearance without changing equipment or combat stats. Each piece
can be equipped separately. The server saves the items and equipped skin slots,
and tells nearby players about the appearance. Server transformations and
Chaos Castle appearances take precedence on your own character.

The previous free, local set selector has been replaced by item ownership. The
Set 315 models and texture ship under `Data/Custom/Skins/Set315`; item definitions
and models are in `Data/Items/Group07_Helm.json` through `Group11_Boots.json` and
their corresponding `Models` files. The Android build copies these with the
other game data. An existing installation needs the updated game data too.

Both the updated mobile client and the matching OpenMU server are required.
In `C:\OpenMU\OpenMU`, apply the **Set 315 Cosmetic Armor** configuration update
for an existing database, or use the transactional
`deploy/sql/cosmetic-armor-set315.sql` script while the local server is stopped.
That script also grants one of each piece to the named test character and safely
skips pieces already owned. Example:

```powershell
Get-Content -Raw deploy/sql/cosmetic-armor-set315.sql |
    docker exec -i database psql -U postgres -d openmu -v ON_ERROR_STOP=1 -v skin_character=testgmDK
```

For the VPS, deploy the server code and updated database together. A database
transfer alone does not install the cosmetic equip handler. Older clients do
not have the new Skin page or Set 315 assets. No VPS deployment is performed by
building the mobile app.

The pack's weapon, wing, cape, dye and custom particle-effect files are not
part of this armor-only version.

Run `python tools/test_cosmetic_armor.py` to check the armor assets against the
player's bone palette and confirm their textures are present. Check fit and
animation in game for each class before distributing the client.
