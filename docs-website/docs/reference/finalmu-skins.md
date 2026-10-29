# FinalMU cosmetic import

This is a partial import of the supplied FinalMU Season6 x1000 v2 client pack.
It adds 24 readable weapon/shield models, 82 readable custom wings, 57 readable followers, and 13 rideable Fenrir mounts.
The items are permanent and tradable. Original IDs are recorded in
`Data/Custom/Skins/FinalMU/catalog.json`; the new IDs avoid all existing items.
Followers use cosmetic following behavior and grant no gameplay benefits.
The 13 models configured as Fenrir in the source pet table are standalone mounts
in the Gear helper slot with normal Fenrir riding, requirements, skill and durability.
Their shop previews include the rider; their price is configured by `MountPrice`.
Portable mesh-glow settings from the pet table are included where present.
The import also includes 212 static light sprites from the readable effect
tables. Another 158 sprite references targeted absent bones and were excluded.
Windows DLL-only effect programs have not been reproduced.

Deploy the matching OpenMU server source, apply **Set 315 Cosmetic Armor version
7**, and restart before creating these items. Existing skins continue to use
their original commands. Enter each command below separately on a GM character,
pick up the item, and use it to equip the corresponding Skin slot. Mounts instead
equip the Gear helper slot and cannot occupy the Skin pet slot. Non-pet skins
receive one default standard bonus; add a single usual excellent flag as the
fourth command argument to choose another. The [Skin Shop](cosmetic-armor.md)
provides previews and a selectable bonus for 100 WCoinC per item by default.

The source scan found 823 configured models in a protected format, 30 models
which fail the current strict BMD validation, 11 missing models and two ambiguous
model names. Those were excluded. Thirty-four readable material/item models
are not wearable skins; twenty readable armor models were withheld because the
active class restrictions are in an unreadable item table. Completing the
entire pack requires unprotected assets or a compatible decoder and the class
and effect definitions. No existing client files or IDs were replaced.

Custom wing and follower playback is interpolated in the client. Wings with fewer
BMD keys retain the same flap cycle duration. All 82 imported wings and 70 pet/mount
models were checked for action keys: Wing204 contains only one key and stays static.
Mount actions use the normal Fenrir cycle timing; missing actions use safe poses.
Existing imported mesh glow and light settings remain active. The client animation
changes need no database update; converting Fenrir items requires version 7.

- `hs_blueeyeknightA Skin`: `/item 0 300`
- `blue_eye_magicsword01 Skin`: `/item 0 301`
- `blastbreak Skin`: `/item 0 302`
- `hs_blueeyelance Skin`: `/item 0 303`
- `Blue_eyeShortSword Skin`: `/item 0 304`
- `soulknightsword Skin`: `/item 0 305`
- `SoulmagicSword Skin`: `/item 0 306`
- `SoulShortSword Skin`: `/item 0 307`
- `HolyAngelShortSword Skin`: `/item 0 308`
- `blessedabsolute_lance Skin`: `/item 0 309`
- `BlessedAbsoluteShortSword Skin`: `/item 0 310`
- `thunder_bolt Skin`: `/item 2 300`
- `hornofsteel Skin`: `/item 2 301`
- `Bless_Absolute_Mace Skin`: `/item 2 302`
- `Axeggggg Skin`: `/item 2 303`
- `Spear100 Skin`: `/item 3 300`
- `Spear101 Skin`: `/item 3 301`
- `blue_eyebow_stand Skin`: `/item 4 300`
- `hs_soularcherbow_s Skin`: `/item 4 301`
- `mastery_staff Skin`: `/item 5 300`
- `blue_eye_ws Skin`: `/item 5 301`
- `blueeye_summoner Skin`: `/item 5 302`
- `sd_SoulWizardStaff01_max7 Skin`: `/item 5 303`
- `sd_soulsummoner Skin`: `/item 5 304`
- `Fenrir1 Mount`: `/item 13 300`
- `Fenrir2 Mount`: `/item 13 301`
- `Fenrir3 Mount`: `/item 13 302`
- `Fenrir4 Mount`: `/item 13 303`
- `Fenrir5 Mount`: `/item 13 304`
- `Fenrir6 Mount`: `/item 13 305`
- `Fenrir7 Mount`: `/item 13 306`
- `Fenrir8 Mount`: `/item 13 307`
- `Fenrir9 Mount`: `/item 13 308`
- `ghost_horse Skin`: `/item 13 309`
- `AwayFenrirBlue Mount`: `/item 13 310`
- `IceDragonSteel Mount`: `/item 13 311`
- `soi Mount`: `/item 13 312`
- `fighterpet_evol Skin`: `/item 13 313`
- `ripieoup Skin`: `/item 13 314`
- `satanpet_evol Skin`: `/item 13 315`
- `seirenpet_evol Skin`: `/item 13 316`
- `sweetmusclemanup Skin`: `/item 13 317`
- `tapppet_evol Skin`: `/item 13 318`
- `treeelfup Skin`: `/item 13 319`
- `witchpet_evol Skin`: `/item 13 320`
- `goalkeeperup Skin`: `/item 13 321`
- `adventurecatup Skin`: `/item 13 322`
- `beachgirlup Skin`: `/item 13 323`
- `bestcaptainup Skin`: `/item 13 324`
- `blackleopardup Skin`: `/item 13 325`
- `bluefairyup Skin`: `/item 13 326`
- `bearhockeyup Skin`: `/item 13 327`
- `branchpet_evol Skin`: `/item 13 328`
- `caniflepet_evol Skin`: `/item 13 329`
- `djmonkeyup Skin`: `/item 13 330`
- `gracchuspet_evol Skin`: `/item 13 331`
- `xmastreeup Skin`: `/item 13 332`
- `pinocchioup Skin`: `/item 13 333`
- `raincoatgirlup Skin`: `/item 13 334`
- `rockapet_evol Skin`: `/item 13 335`
- `wooparoopet_evol Skin`: `/item 13 336`
- `treatpet_evol Skin`: `/item 13 337`
- `treasurehunterup Skin`: `/item 13 338`
- `toyrobotup Skin`: `/item 13 339`
- `tobypet_evol Skin`: `/item 13 340`
- `surfinggirlup Skin`: `/item 13 341`
- `superpandaup Skin`: `/item 13 342`
- `skullpet_evol Skin`: `/item 13 343`
- `scarecrowup Skin`: `/item 13 344`
- `reynerpet_evol Skin`: `/item 13 345`
- `mutpet_evol Skin`: `/item 13 346`
- `mutationpet_evol Skin`: `/item 13 347`
- `mermaidpet_evol Skin`: `/item 13 348`
- `libraup Skin`: `/item 13 349`
- `kenturionpet_evol Skin`: `/item 13 350`
- `indioup Skin`: `/item 13 351`
- `chefchickenup Skin`: `/item 13 352`
- `pilotup Skin`: `/item 13 353`
- `angrychickenup Skin`: `/item 13 354`
- `babybearup Skin`: `/item 13 355`
- `baelpet_evol Skin`: `/item 13 356`
- `beagleup Skin`: `/item 13 357`
- `behemothpet_evol Skin`: `/item 13 358`
- `bellydancerup Skin`: `/item 13 359`
- `bogeonpet_evol Skin`: `/item 13 360`
- `bulldogup Skin`: `/item 13 361`
- `caniflepet_evol Skin`: `/item 13 362`
- `cherryblossompet_evol Skin`: `/item 13 363`
- `chococookieup Skin`: `/item 13 364`
- `Collos3 Skin`: `/item 13 365`
- `Collos2 Skin`: `/item 13 366`
- `IceQueen Skin`: `/item 13 367`
- `fiercelionR Mount`: `/item 13 368`
- `Horse10 Skin`: `/item 13 369`

- `GuncrasherWing3rd Wing Skin`: `/item 12 300`
- `GuncrasherWing4th Wing Skin`: `/item 12 301`
- `KCW_WingP Wing Skin`: `/item 12 302`
- `KCW_WingR Wing Skin`: `/item 12 303`
- `MJS_wing_12 Wing Skin`: `/item 12 304`
- `Wing1000 Wing Skin`: `/item 12 305`
- `wing1100 Wing Skin`: `/item 12 306`
- `Wing1300 Wing Skin`: `/item 12 307`
- `Wing1400 Wing Skin`: `/item 12 308`
- `wing180 Wing Skin`: `/item 12 309`
- `Wing204 Wing Skin`: `/item 12 310`
- `Wing52 Wing Skin`: `/item 12 311`
- `Wing53 Wing Skin`: `/item 12 312`
- `Wing54 Wing Skin`: `/item 12 313`
- `Wing55 Wing Skin`: `/item 12 314`
- `wing7mg Wing Skin`: `/item 12 315`
- `WingElf7 Wing Skin`: `/item 12 316`
- `Wings1 Wing Skin`: `/item 12 317`
- `Wings10 Wing Skin`: `/item 12 318`
- `Wings11-copy Wing Skin`: `/item 12 319`
- `Wings11 Wing Skin`: `/item 12 320`
- `Wings12 Wing Skin`: `/item 12 321`
- `Wings13 Wing Skin`: `/item 12 322`
- `Wings14 Wing Skin`: `/item 12 323`
- `Wings15 Wing Skin`: `/item 12 324`
- `Wings16 Wing Skin`: `/item 12 325`
- `Wings17 Wing Skin`: `/item 12 326`
- `Wings18 Wing Skin`: `/item 12 327`
- `Wings19 Wing Skin`: `/item 12 328`
- `Wings2 Wing Skin`: `/item 12 329`
- `Wings20 Wing Skin`: `/item 12 330`
- `Wings21-copy Wing Skin`: `/item 12 331`
- `Wings21 Wing Skin`: `/item 12 332`
- `Wings22-copy Wing Skin`: `/item 12 333`
- `Wings22 Wing Skin`: `/item 12 334`
- `Wings23 Wing Skin`: `/item 12 335`
- `Wings24 Wing Skin`: `/item 12 336`
- `Wings25 Wing Skin`: `/item 12 337`
- `Wings26-coy Wing Skin`: `/item 12 338`
- `Wings26 Wing Skin`: `/item 12 339`
- `Wings27 Wing Skin`: `/item 12 340`
- `Wings28-new Wing Skin`: `/item 12 341`
- `Wings28 Wing Skin`: `/item 12 342`
- `Wings29 Wing Skin`: `/item 12 343`
- `Wings3-copy Wing Skin`: `/item 12 344`
- `Wings3 Wing Skin`: `/item 12 345`
- `Wings30-new Wing Skin`: `/item 12 346`
- `Wings30 Wing Skin`: `/item 12 347`
- `Wings31 Wing Skin`: `/item 12 348`
- `Wings32 Wing Skin`: `/item 12 349`
- `Wings4-copy Wing Skin`: `/item 12 350`
- `Wings4 Wing Skin`: `/item 12 351`
- `Wings5 Wing Skin`: `/item 12 352`
- `Wings7 Wing Skin`: `/item 12 353`
- `Wings77 Wing Skin`: `/item 12 354`
- `Wings8-copy Wing Skin`: `/item 12 355`
- `Wings8 Wing Skin`: `/item 12 356`
- `Wings9 Wing Skin`: `/item 12 357`
- `WingsOfPower Wing Skin`: `/item 12 358`
- `DK_Wings5 Wing Skin`: `/item 12 359`
- `DL_Wings5 Wing Skin`: `/item 12 360`
- `DW_Wings5 Wing Skin`: `/item 12 361`
- `Elf_Wings5 Wing Skin`: `/item 12 362`
- `MG_Wings5 Wing Skin`: `/item 12 363`
- `MG_Wings5_2 Wing Skin`: `/item 12 364`
- `SCF_WINGS211 Wing Skin`: `/item 12 365`
- `Sum_Wings5 Wing Skin`: `/item 12 366`
- `Wing-Pawi Wing Skin`: `/item 12 367`
- `Wing Wing Skin`: `/item 12 368`
- `Wing03 Wing Skin`: `/item 12 369`
- `Wing118 Wing Skin`: `/item 12 370`
- `Wing183 Wing Skin`: `/item 12 371`
- `Wing200 Wing Skin`: `/item 12 372`
- `Wing2222 Wing Skin`: `/item 12 373`
- `Wing223 Wing Skin`: `/item 12 374`
- `Wing408 Wing Skin`: `/item 12 375`
- `WingBlack Wing Skin`: `/item 12 376`
- `WingofGhost Wing Skin`: `/item 12 377`
- `WingsOfPower Wing Skin`: `/item 12 378`
- `WingWhite Wing Skin`: `/item 12 379`
- `angel_devil_wing Wing Skin`: `/item 12 380`
- `ConquerorWing Wing Skin`: `/item 12 381`
