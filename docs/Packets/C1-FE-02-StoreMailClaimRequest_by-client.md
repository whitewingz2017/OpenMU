# C1 FE 02 - StoreMailClaimRequest (by client)

## Is sent when

A TwinMU personal-store or mail operation is requested.

## Causes the following actions on the server side

The server validates ownership and all payment components.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   6   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFE  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x02  | Packet header - sub packet type identifier |
| 4 | 2 | ShortLittleEndian |  | LetterIndex |