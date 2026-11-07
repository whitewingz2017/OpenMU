# C2 FD 07 - RankTitleAppearanceExtended (by server)

## Is sent when

A player enters view or changes their equipped rank title.

## Causes the following actions on the client side

The custom client reads UTF-8 JSON with a Ranks array containing Id, Name and Rank for each player. Rank zero clears the title.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x07  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |