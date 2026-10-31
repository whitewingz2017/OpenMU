# Boss-only socket equipment drops

After rebuilding/deploying the server, enable **Boss-only socket drops** under
Game configuration > Plugins. No client update or database migration is required.

The filter blocks all monster-dropped socket-capable equipment (even with zero
sockets), including random, excellent, map and explicit monster drop groups.
Regular items are unaffected. Existing boss drop chances are preserved: this
restriction does not add guaranteed drops or new loot tables.

Edit **Boss monster numbers** in the plugin configuration for custom bosses.
Defaults include the golden invasion bosses, Red Dragon, White Wizard, Balrog, Hydra, Phoenix, Kundun, Erohim, Dark Elf, Balgass,
Kanturu bosses, Selupan, Medusa, Imperial Guardian bosses 504–511 (including
Gaion), and Doppelganger butchers 529–530. Devil Square maps 9/32 allow only
OnceAtWaveStart spawns in boss wave 10, so ordinary waves remain blocked even
if they reuse a boss monster definition. Custom event maps/waves require matching
configuration. Monster IDs, rather than localized display names, determine eligibility.

This filters drops from killed monsters. Boxes, crafting, shop purchases and
separate event completion rewards are unchanged. Existing socket equipment is
not removed from inventories.
