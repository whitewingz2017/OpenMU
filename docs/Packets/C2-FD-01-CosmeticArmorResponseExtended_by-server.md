# C2 FD 01 - CosmeticArmorResponseExtended (by server)

## Is sent when

A cosmetic armor request was processed.

## Causes the following actions on the client side

The custom client replaces its cosmetic slots using the UTF-8 JSON snapshot containing RequestId, Status and Skins entries with Part and ItemType. It never changes combat equipment.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x01  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |