# Duel betting

Enable the **Duel Betting** feature plugin and apply the `DuelBetEscrow`
database migration to offer public betting to custom clients. Enable the
Jewel Bank and configure the offered currencies there as well. The betting
plugin controls the pre-combat window (30 seconds by default), stake limits,
pool limits, and fee. Remove currency identifier 3 from any existing custom
configuration. The supported currencies are Bless, Soul, and Life.

The challenger chooses whether to allow betting for each invitation. Once both
fighters accept and enter the arena, a public challenge is announced to its
game channel and listed in **Menu > Duel Betting**. Players on that channel can
back one fighter from any map. The two duelist accounts are ineligible. Regular
duels use their usual countdown. The invitation shows the server countdown and
the score required to win. In the arena board, **View Details** shows the matchup
and the share of stakes in the selected currency. **Spectate Duel** joins an
ongoing match when its spectator area has space. **Completed** and **My Bets**
show results and wagers for rooms still listed by the channel.

Bets close before combat begins. Each currency has its own pool. With no fee,
20 Bless on each fighter pays 40 Bless to the winning bettor. Multiple winners
share the pool in proportion to their stakes, including their initial stake.
Fees round down, and any remaining whole units go to the largest fractional
remainders, with ticket identity breaking ties.

Cancelled matches and pools with stakes on only one side refund the original
stakes. Use **Claim payouts** in the betting board to transfer payouts and
refunds to your account's Jewel Bank. You can claim after reconnecting. If the
server stops before it records a result, the original stakes are refundable
once the match's escrow lease expires, within two minutes of its last renewal.
