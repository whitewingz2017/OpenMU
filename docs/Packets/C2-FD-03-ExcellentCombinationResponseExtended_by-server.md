# C2 FD 03 - ExcellentCombinationResponseExtended (by server)

## Is sent when

A combination quote or confirmation completes.

## Causes the following actions on the client side

The client displays RequestId, Status, Valid, Success, Cost and ExcellentFlags from UTF-8 JSON after inventory synchronization.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x03  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |