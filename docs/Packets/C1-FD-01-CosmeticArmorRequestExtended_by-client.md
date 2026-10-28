# C1 FD 01 - CosmeticArmorRequestExtended (by client)

## Is sent when

The custom client opens its Skin page or equips or unequips an owned cosmetic item.

## Causes the following actions on the server side

The server validates ownership and the configured cosmetic slot, persists the move, refreshes inventory and returns confirmed skin slots. Operations are 0 refresh, 1 equip from InventorySlot, 2 unequip Part. Part is 0 helmet through 4 boots.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   11   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x01  | Packet header - sub packet type identifier |
| 4 | 1 | Byte |  | Operation |
| 5 | 1 | Byte |  | InventorySlot |
| 6 | 1 | Byte |  | Part |
| 7 | 4 | IntegerLittleEndian |  | RequestId |