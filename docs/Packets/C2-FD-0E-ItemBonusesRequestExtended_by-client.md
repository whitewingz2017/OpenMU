# C2 FD 0E - ItemBonusesRequestExtended (by client)

## Is sent when

The custom client loads the attribute lines of the items picked in the Item Attributes plugin (0), once for each character.

## Causes the following actions on the server side

The server answers with the matching RequestId.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x0E  | Packet header - sub packet type identifier |
| 5 | 1 | Byte |  | Operation |
| 6 | 4 | IntegerLittleEndian |  | RequestId |
| 10 |  | Binary |  | Data |