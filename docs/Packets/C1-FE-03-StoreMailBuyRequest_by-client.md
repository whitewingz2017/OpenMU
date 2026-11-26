# C1 FE 03 - StoreMailBuyRequest (by client)

## Is sent when

The buyer confirms a personal-store purchase for mail delivery.

## Causes the following actions on the server side

All required currencies are charged and the exact item is attached to buyer mail atomically.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   17   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFE  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x03  | Packet header - sub packet type identifier |
| 4 | 2 | ShortLittleEndian |  | PlayerId |
| 6 | 10 | String |  | PlayerName |
| 16 | 1 | Byte |  | Slot |