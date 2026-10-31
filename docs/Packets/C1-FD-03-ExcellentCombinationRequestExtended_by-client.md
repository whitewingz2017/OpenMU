# C1 FD 03 - ExcellentCombinationRequestExtended (by client)

## Is sent when

The client quotes (0) or confirms (1) two inventory items.

## Causes the following actions on the server side

The server validates the quoted identities, combines at most two excellent options and atomically saves the primary item, material removal and Zen.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   11   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x03  | Packet header - sub packet type identifier |
| 4 | 1 | Byte |  | Operation |
| 5 | 1 | Byte |  | PrimarySlot |
| 6 | 1 | Byte |  | SecondarySlot |
| 7 | 4 | IntegerLittleEndian |  | RequestId |