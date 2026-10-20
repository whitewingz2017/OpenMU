# C1 D2 11 - CashShopPeriodItemCount (by server)

## Is sent when

The inventory is sent and its period-item expiration list follows.

## Causes the following actions on the client side

The client refreshes its expiration metadata for the following inventory entries.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   5   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xD2  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x11  | Packet header - sub packet type identifier |
| 4 | 1 | Byte |  | ItemCount |