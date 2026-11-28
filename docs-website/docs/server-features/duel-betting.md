# Duel betting

In OpenMU, go to **Configuration → Plugins → Feature Plugins**, enable
**Duel Betting**, and apply the `DuelBetEscrow`
database migration to offer public betting to custom clients. The plugin
controls the pre-combat window (30 seconds by default), the total stake cap,
and the ticket limit per account. `WCoinLimit` controls the permitted WCoinC
amount per fighter and per spectator ticket. The default range is 1–250 WCoinC.

In **Duel Setup**, the challenger can set a WCoinC stake per fighter. The
opponent sees the stake and pot before accepting. Both accounts must cover
their stake when the opponent accepts. The winner receives both stakes.

Once both fighters enter the arena, a public challenge is announced to its
game channel and listed in **Menu > Duel Betting**. Players on that channel can
back one fighter from any map. The two duelist accounts are ineligible. Regular
duels use their usual countdown. The invitation shows the server countdown and
the score required to win. In the arena board, **View Details** shows the matchup
and the payout terms. **Spectate Duel** joins an
ongoing match when its spectator area has space. **Completed** and **My Bets**
show results and wagers for rooms still listed by the channel. **Ongoing** also
lists regular fights once combat starts. Duel Setup explains when the server
cannot accept wagers.

Bets close before combat begins. Each correct WCoinC pick pays twice the
spectator's stake, including the original stake, even in a one-sided market.
Losing bets pay zero. The Cash Shop account balance funds stakes and receives
credits. Older clients can still start jewel pools, which use the configured
jewel limits and proportional payouts. Currency identifier 3 remains retired.

Canceled matches refund the original stakes. WCoinC credits are delivered to
online recipients. Use **Claim Payouts** in the betting board to collect
outstanding payouts and refunds after reconnecting. If the
server stops before it records a result, the original stakes are refundable
once the match's escrow lease expires, within two minutes of its last renewal.
