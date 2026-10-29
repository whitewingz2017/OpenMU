# C1 FD 02 - SkinShopRequestExtended (by client)

## Is sent when

The custom client requests the skin catalog (0) or buys one skin with a chosen bonus (1).

## Causes the following actions on the server side

The server validates the configured WCoinC price, bonus, class, ownership storage and replay sequence, saves the purchase atomically and returns the catalog or result.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   16   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x02  | Packet header - sub packet type identifier |
| 4 | 1 | Byte |  | Operation |
| 5 | 2 | ShortLittleEndian |  | ItemType |
| 7 | 1 | Byte |  | Bonus |
| 8 | 4 | IntegerLittleEndian |  | QuotedPrice |
| 12 | 4 | IntegerLittleEndian |  | RequestId |