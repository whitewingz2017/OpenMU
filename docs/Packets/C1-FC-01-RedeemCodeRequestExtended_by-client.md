# C1 FC 01 - RedeemCodeRequestExtended (by client)

## Is sent when

The custom mobile client submits a gift code.

## Causes the following actions on the server side

The server validates usage and expiry, saves rewards and a receipt atomically, and returns a system message.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   36   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFC  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x01  | Packet header - sub packet type identifier |
| 4 | 32 | String |  | CodeText |