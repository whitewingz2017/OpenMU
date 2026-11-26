# C2 FD 10 - DuelBettingResponseExtended (by server)

## Is sent when

A Duel Arena request was processed, or immediately before the standard duel invitation packet for a custom client.

## Causes the following actions on the client side

The client reads UTF-8 JSON with RequestId, Enabled, localized Status, the countdown and winning-score settings, and Duels with fighters, classes, scores, time, status, eligibility and per-currency pools. An Invitation object instead supplies the challenger, their class, public betting choice, seconds and winning score for the standard invitation.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x10  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |