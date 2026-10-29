# FinalMU cosmetic import

This is a partial import of the supplied FinalMU Season6 x1000 v2 client pack.
It adds 24 readable weapon/shield models, 82 readable custom wings, and 70 readable pets as permanent,
tradable cosmetics. Original IDs are recorded in
`Data/Custom/Skins/FinalMU/catalog.json`; the new IDs avoid all existing items.
The pet models use a cosmetic following behavior. They do not grant mount
movement, attacks, item collection, stats, or other FinalMU gameplay benefits.
Portable mesh-glow settings from the pet table are included where present.
The import also includes 212 static light sprites from the readable effect
tables. Another 158 sprite references targeted absent bones and were excluded.
Windows DLL-only effect programs have not been reproduced.

Deploy the matching OpenMU server source, apply **Set 315 Cosmetic Armor version
5**, and restart before creating these items. Existing skins continue to use
their original commands. Enter each command below separately on a GM character,
pick up the item, and use it to equip the corresponding Skin slot.

The source scan found 823 configured models in a protected format, 30 models
which fail the current strict BMD validation, 11 missing models and two ambiguous
model names. Those were excluded. Thirty-four readable material/item models
are not wearable skins; twenty readable armor models were withheld because the
active class restrictions are in an unreadable item table. Completing the
entire pack requires unprotected assets or a compatible decoder and the class
and effect definitions. No existing client files or IDs were replaced.

The separate Skin slot visibility and normal wing animation fixes are included
in the mobile client. Neither requires a server data change.

- `FinalMU hs_blueeyeknightA Skin`: `/item 0 300`
- `FinalMU blue_eye_magicsword01 Skin`: `/item 0 301`
- `FinalMU blastbreak Skin`: `/item 0 302`
- `FinalMU hs_blueeyelance Skin`: `/item 0 303`
- `FinalMU Blue_eyeShortSword Skin`: `/item 0 304`
- `FinalMU soulknightsword Skin`: `/item 0 305`
- `FinalMU SoulmagicSword Skin`: `/item 0 306`
- `FinalMU SoulShortSword Skin`: `/item 0 307`
- `FinalMU HolyAngelShortSword Skin`: `/item 0 308`
- `FinalMU blessedabsolute_lance Skin`: `/item 0 309`
- `FinalMU BlessedAbsoluteShortSword Skin`: `/item 0 310`
- `FinalMU thunder_bolt Skin`: `/item 2 300`
- `FinalMU hornofsteel Skin`: `/item 2 301`
- `FinalMU Bless_Absolute_Mace Skin`: `/item 2 302`
- `FinalMU Axeggggg Skin`: `/item 2 303`
- `FinalMU Spear100 Skin`: `/item 3 300`
- `FinalMU Spear101 Skin`: `/item 3 301`
- `FinalMU blue_eyebow_stand Skin`: `/item 4 300`
- `FinalMU hs_soularcherbow_s Skin`: `/item 4 301`
- `FinalMU mastery_staff Skin`: `/item 5 300`
- `FinalMU blue_eye_ws Skin`: `/item 5 301`
- `FinalMU blueeye_summoner Skin`: `/item 5 302`
- `FinalMU sd_SoulWizardStaff01_max7 Skin`: `/item 5 303`
- `FinalMU sd_soulsummoner Skin`: `/item 5 304`
- `FinalMU Fenrir1 Skin`: `/item 13 300`
- `FinalMU Fenrir2 Skin`: `/item 13 301`
- `FinalMU Fenrir3 Skin`: `/item 13 302`
- `FinalMU Fenrir4 Skin`: `/item 13 303`
- `FinalMU Fenrir5 Skin`: `/item 13 304`
- `FinalMU Fenrir6 Skin`: `/item 13 305`
- `FinalMU Fenrir7 Skin`: `/item 13 306`
- `FinalMU Fenrir8 Skin`: `/item 13 307`
- `FinalMU Fenrir9 Skin`: `/item 13 308`
- `FinalMU ghost_horse Skin`: `/item 13 309`
- `FinalMU AwayFenrirBlue Skin`: `/item 13 310`
- `FinalMU IceDragonSteel Skin`: `/item 13 311`
- `FinalMU soi Skin`: `/item 13 312`
- `FinalMU fighterpet_evol Skin`: `/item 13 313`
- `FinalMU ripieoup Skin`: `/item 13 314`
- `FinalMU satanpet_evol Skin`: `/item 13 315`
- `FinalMU seirenpet_evol Skin`: `/item 13 316`
- `FinalMU sweetmusclemanup Skin`: `/item 13 317`
- `FinalMU tapppet_evol Skin`: `/item 13 318`
- `FinalMU treeelfup Skin`: `/item 13 319`
- `FinalMU witchpet_evol Skin`: `/item 13 320`
- `FinalMU goalkeeperup Skin`: `/item 13 321`
- `FinalMU adventurecatup Skin`: `/item 13 322`
- `FinalMU beachgirlup Skin`: `/item 13 323`
- `FinalMU bestcaptainup Skin`: `/item 13 324`
- `FinalMU blackleopardup Skin`: `/item 13 325`
- `FinalMU bluefairyup Skin`: `/item 13 326`
- `FinalMU bearhockeyup Skin`: `/item 13 327`
- `FinalMU branchpet_evol Skin`: `/item 13 328`
- `FinalMU caniflepet_evol Skin`: `/item 13 329`
- `FinalMU djmonkeyup Skin`: `/item 13 330`
- `FinalMU gracchuspet_evol Skin`: `/item 13 331`
- `FinalMU xmastreeup Skin`: `/item 13 332`
- `FinalMU pinocchioup Skin`: `/item 13 333`
- `FinalMU raincoatgirlup Skin`: `/item 13 334`
- `FinalMU rockapet_evol Skin`: `/item 13 335`
- `FinalMU wooparoopet_evol Skin`: `/item 13 336`
- `FinalMU treatpet_evol Skin`: `/item 13 337`
- `FinalMU treasurehunterup Skin`: `/item 13 338`
- `FinalMU toyrobotup Skin`: `/item 13 339`
- `FinalMU tobypet_evol Skin`: `/item 13 340`
- `FinalMU surfinggirlup Skin`: `/item 13 341`
- `FinalMU superpandaup Skin`: `/item 13 342`
- `FinalMU skullpet_evol Skin`: `/item 13 343`
- `FinalMU scarecrowup Skin`: `/item 13 344`
- `FinalMU reynerpet_evol Skin`: `/item 13 345`
- `FinalMU mutpet_evol Skin`: `/item 13 346`
- `FinalMU mutationpet_evol Skin`: `/item 13 347`
- `FinalMU mermaidpet_evol Skin`: `/item 13 348`
- `FinalMU libraup Skin`: `/item 13 349`
- `FinalMU kenturionpet_evol Skin`: `/item 13 350`
- `FinalMU indioup Skin`: `/item 13 351`
- `FinalMU chefchickenup Skin`: `/item 13 352`
- `FinalMU pilotup Skin`: `/item 13 353`
- `FinalMU angrychickenup Skin`: `/item 13 354`
- `FinalMU babybearup Skin`: `/item 13 355`
- `FinalMU baelpet_evol Skin`: `/item 13 356`
- `FinalMU beagleup Skin`: `/item 13 357`
- `FinalMU behemothpet_evol Skin`: `/item 13 358`
- `FinalMU bellydancerup Skin`: `/item 13 359`
- `FinalMU bogeonpet_evol Skin`: `/item 13 360`
- `FinalMU bulldogup Skin`: `/item 13 361`
- `FinalMU caniflepet_evol Skin`: `/item 13 362`
- `FinalMU cherryblossompet_evol Skin`: `/item 13 363`
- `FinalMU chococookieup Skin`: `/item 13 364`
- `FinalMU Collos3 Skin`: `/item 13 365`
- `FinalMU Collos2 Skin`: `/item 13 366`
- `FinalMU IceQueen Skin`: `/item 13 367`
- `FinalMU fiercelionR Skin`: `/item 13 368`
- `FinalMU Horse10 Skin`: `/item 13 369`

- `FinalMU GuncrasherWing3rd Wing Skin`: `/item 12 300`
- `FinalMU GuncrasherWing4th Wing Skin`: `/item 12 301`
- `FinalMU KCW_WingP Wing Skin`: `/item 12 302`
- `FinalMU KCW_WingR Wing Skin`: `/item 12 303`
- `FinalMU MJS_wing_12 Wing Skin`: `/item 12 304`
- `FinalMU Wing1000 Wing Skin`: `/item 12 305`
- `FinalMU wing1100 Wing Skin`: `/item 12 306`
- `FinalMU Wing1300 Wing Skin`: `/item 12 307`
- `FinalMU Wing1400 Wing Skin`: `/item 12 308`
- `FinalMU wing180 Wing Skin`: `/item 12 309`
- `FinalMU Wing204 Wing Skin`: `/item 12 310`
- `FinalMU Wing52 Wing Skin`: `/item 12 311`
- `FinalMU Wing53 Wing Skin`: `/item 12 312`
- `FinalMU Wing54 Wing Skin`: `/item 12 313`
- `FinalMU Wing55 Wing Skin`: `/item 12 314`
- `FinalMU wing7mg Wing Skin`: `/item 12 315`
- `FinalMU WingElf7 Wing Skin`: `/item 12 316`
- `FinalMU Wings1 Wing Skin`: `/item 12 317`
- `FinalMU Wings10 Wing Skin`: `/item 12 318`
- `FinalMU Wings11-copy Wing Skin`: `/item 12 319`
- `FinalMU Wings11 Wing Skin`: `/item 12 320`
- `FinalMU Wings12 Wing Skin`: `/item 12 321`
- `FinalMU Wings13 Wing Skin`: `/item 12 322`
- `FinalMU Wings14 Wing Skin`: `/item 12 323`
- `FinalMU Wings15 Wing Skin`: `/item 12 324`
- `FinalMU Wings16 Wing Skin`: `/item 12 325`
- `FinalMU Wings17 Wing Skin`: `/item 12 326`
- `FinalMU Wings18 Wing Skin`: `/item 12 327`
- `FinalMU Wings19 Wing Skin`: `/item 12 328`
- `FinalMU Wings2 Wing Skin`: `/item 12 329`
- `FinalMU Wings20 Wing Skin`: `/item 12 330`
- `FinalMU Wings21-copy Wing Skin`: `/item 12 331`
- `FinalMU Wings21 Wing Skin`: `/item 12 332`
- `FinalMU Wings22-copy Wing Skin`: `/item 12 333`
- `FinalMU Wings22 Wing Skin`: `/item 12 334`
- `FinalMU Wings23 Wing Skin`: `/item 12 335`
- `FinalMU Wings24 Wing Skin`: `/item 12 336`
- `FinalMU Wings25 Wing Skin`: `/item 12 337`
- `FinalMU Wings26-coy Wing Skin`: `/item 12 338`
- `FinalMU Wings26 Wing Skin`: `/item 12 339`
- `FinalMU Wings27 Wing Skin`: `/item 12 340`
- `FinalMU Wings28-new Wing Skin`: `/item 12 341`
- `FinalMU Wings28 Wing Skin`: `/item 12 342`
- `FinalMU Wings29 Wing Skin`: `/item 12 343`
- `FinalMU Wings3-copy Wing Skin`: `/item 12 344`
- `FinalMU Wings3 Wing Skin`: `/item 12 345`
- `FinalMU Wings30-new Wing Skin`: `/item 12 346`
- `FinalMU Wings30 Wing Skin`: `/item 12 347`
- `FinalMU Wings31 Wing Skin`: `/item 12 348`
- `FinalMU Wings32 Wing Skin`: `/item 12 349`
- `FinalMU Wings4-copy Wing Skin`: `/item 12 350`
- `FinalMU Wings4 Wing Skin`: `/item 12 351`
- `FinalMU Wings5 Wing Skin`: `/item 12 352`
- `FinalMU Wings7 Wing Skin`: `/item 12 353`
- `FinalMU Wings77 Wing Skin`: `/item 12 354`
- `FinalMU Wings8-copy Wing Skin`: `/item 12 355`
- `FinalMU Wings8 Wing Skin`: `/item 12 356`
- `FinalMU Wings9 Wing Skin`: `/item 12 357`
- `FinalMU WingsOfPower Wing Skin`: `/item 12 358`
- `FinalMU DK_Wings5 Wing Skin`: `/item 12 359`
- `FinalMU DL_Wings5 Wing Skin`: `/item 12 360`
- `FinalMU DW_Wings5 Wing Skin`: `/item 12 361`
- `FinalMU Elf_Wings5 Wing Skin`: `/item 12 362`
- `FinalMU MG_Wings5 Wing Skin`: `/item 12 363`
- `FinalMU MG_Wings5_2 Wing Skin`: `/item 12 364`
- `FinalMU SCF_WINGS211 Wing Skin`: `/item 12 365`
- `FinalMU Sum_Wings5 Wing Skin`: `/item 12 366`
- `FinalMU Wing-Pawi Wing Skin`: `/item 12 367`
- `FinalMU Wing Wing Skin`: `/item 12 368`
- `FinalMU Wing03 Wing Skin`: `/item 12 369`
- `FinalMU Wing118 Wing Skin`: `/item 12 370`
- `FinalMU Wing183 Wing Skin`: `/item 12 371`
- `FinalMU Wing200 Wing Skin`: `/item 12 372`
- `FinalMU Wing2222 Wing Skin`: `/item 12 373`
- `FinalMU Wing223 Wing Skin`: `/item 12 374`
- `FinalMU Wing408 Wing Skin`: `/item 12 375`
- `FinalMU WingBlack Wing Skin`: `/item 12 376`
- `FinalMU WingofGhost Wing Skin`: `/item 12 377`
- `FinalMU WingsOfPower Wing Skin`: `/item 12 378`
- `FinalMU WingWhite Wing Skin`: `/item 12 379`
- `FinalMU angel_devil_wing Wing Skin`: `/item 12 380`
- `FinalMU ConquerorWing Wing Skin`: `/item 12 381`
