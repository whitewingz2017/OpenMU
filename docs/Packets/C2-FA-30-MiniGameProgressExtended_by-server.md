# C2 FA 30 - MiniGameProgressExtended (by server)

## Is sent when

A Devil Square participant receives a live event progress snapshot once per second.

## Causes the following actions on the client side

The mobile event journal displays the server timer, configured wave progress, score and rank. Data is version 1: version byte, event byte (1 Devil Square), level byte, ended byte, remaining seconds UInt16 LE, points Int32 LE, rank byte, participants byte, wave count byte. Each wave contains number byte, state byte (0 pending, 1 active, 2 completed), starts-in UInt16 LE and ends-in UInt16 LE. At most eight waves are sent.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFA  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x30  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |