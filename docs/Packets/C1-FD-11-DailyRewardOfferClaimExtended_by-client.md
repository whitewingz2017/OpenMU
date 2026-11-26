# C1 FD 11 - DailyRewardOfferClaimExtended (by client)

## Is sent when

The custom client claims a daily reward bundle (Choice 0) or selects one of the displayed items (one-based Choice).

## Causes the following actions on the server side

The server matches OfferKey against the current reward, validates the choice and saves the reward atomically. It returns the calendar with the matching RequestId.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   14   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x11  | Packet header - sub packet type identifier |
| 4 | 1 | Byte |  | Day |
| 5 | 1 | Byte |  | Choice |
| 6 | 4 | IntegerLittleEndian |  | OfferKey |
| 10 | 4 | IntegerLittleEndian |  | RequestId |