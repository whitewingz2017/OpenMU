# C1 FB 20 - EventTimerRequestExtended (by client)

## Is sent when

The custom client opens or refreshes its event timer.

## Causes the following actions on the server side

The server returns upcoming configured events for the current game server.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   8   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFB  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x20  | Packet header - sub packet type identifier |
| 4 | 4 | IntegerLittleEndian |  | RequestId |