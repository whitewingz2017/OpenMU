# C1 FD 08 - RankTitleRequestExtended (by client)

## Is sent when

The custom client loads the rank shop (0), purchases a permanent title (1), or equips an owned title or zero to unequip (2).

## Causes the following actions on the server side

The server checks ownership, the quoted WCoinC price, account balance and replay sequence. It saves purchases atomically and sends the entire shop with the matching RequestId.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   15   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x08  | Packet header - sub packet type identifier |
| 4 | 1 | Byte |  | Operation |
| 5 | 2 | ShortLittleEndian |  | Rank |
| 7 | 4 | IntegerLittleEndian |  | QuotedPrice |
| 11 | 4 | IntegerLittleEndian |  | RequestId |