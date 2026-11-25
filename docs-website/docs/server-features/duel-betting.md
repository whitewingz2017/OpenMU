# Duel betting — development branch

The `codex/duel-betting` branch starts an opt-in feature for Bless, Soul, Life,
and WCoinC. This first slice contains configuration, confirmed-ticket validation,
exact settlement calculations, and the Windows/mobile board interface.

**Real stakes are not enabled.** No packet handler debits currency, no duel lifecycle
creates a bet pool, and no live notification is sent in this slice. Enabling the
experimental configuration plugin does not enable payments.

The intended flow is a notification when an accepted duel opens betting, a board
with the fighters and scores, and a countdown before combat. Each bettor chooses
one fighter and currency. Duelists' accounts cannot bet on their own match.
Betting closes at the deadline or earlier when combat begins.

Each currency forms its own pool. With no fee, 20 Bless on each fighter pays 40
Bless to the winning bettor. Multiple winners share the pool in proportion to
their confirmed stakes. Payouts include the original stake. Integer rounding uses
the largest fractional remainders, with ticket identity breaking ties, so every
distributable unit is assigned exactly once. Fees round down in each currency.
Cancelled matches and pools with bets on only one side refund all stakes without
a fee. Captured settings cannot change when an administrator edits the defaults.

Before this can be enabled for players, the branch still needs persistent escrow
and transactional wallet debit, startup recovery, claimable payouts for full bags,
duelist matched stakes, duel/forfeit hooks, generated protocol packets, and the
live notification/board feed. Integration tests must cover failed saves, duplicate
requests and settlements, reconnects, and a restart with unsettled bets. Merging
the initial slice is not a production betting deployment.
