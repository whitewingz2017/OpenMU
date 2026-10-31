# C2 FD 05 - MuunResponseExtended (by server)

## Is sent when

A custom client requests Muun operations or a Muun appearance changes.

## Causes the following actions on the client side

Operations: 0 refresh, 1 deposit bag source, 2 withdraw stored source, 3 equip stored source to target 0 or 1, 4 unequip source, 5 feed target duplicate to source, 6 evolve source using target bag stone, 7 repair source using target bag material. Responses contain authoritative UTF-8 JSON. Appearance packets identify the player and two visible pet types.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x05  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |