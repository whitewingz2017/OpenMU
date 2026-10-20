# C1 D2 12 - CashShopPeriodItem (by server)

## Is sent when

A timed item appears in the inventory, moves to an inventory slot, or follows the inventory list.

## Causes the following actions on the client side

The client stores its UTC Unix expiration timestamp and displays the remaining time.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   12   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xD2  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x12  | Packet header - sub packet type identifier |
| 4 | 2 | ShortLittleEndian |  | ItemCode |
| 6 | 2 | ShortLittleEndian |  | InventorySlot |
| 8 | 4 | IntegerLittleEndian |  | ExpiresAt |