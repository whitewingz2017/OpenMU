# C1 FD 09 - DailyRewardRequestExtended (by client)

## Is sent when

The custom client loads the daily login reward calendar (0) or claims a single-item or currency-only reward of the given day of the current month (1).

## Causes the following actions on the server side

The server checks the server's current day, the account's claimed days, inventory room and the replay sequence. It grants the configured zen, cash shop coins and item atomically and sends the calendar with the matching RequestId.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   10   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x09  | Packet header - sub packet type identifier |
| 4 | 1 | Byte |  | Operation |
| 5 | 1 | Byte |  | Day |
| 6 | 4 | IntegerLittleEndian |  | RequestId |