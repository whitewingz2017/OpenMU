# C2 FD 08 - RankTitleResponseExtended (by server)

## Is sent when

A rank shop request has been processed.

## Causes the following actions on the client side

The client displays UTF-8 JSON containing RequestId, Status, Balance (account WCoinC), Active and Ranks (Rank, Price, Owned and Name).

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x08  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |