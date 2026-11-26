# C1 FD 05 - MuunRequestExtended (by client)

## Is sent when

A custom client requests Muun operations or a Muun appearance changes.

## Causes the following actions on the server side

Operations: 0 refresh, 1 deposit bag source, 2 withdraw stored source, 3 equip stored source to target 0 or 1, 4 unequip source, 5 feed target duplicate to source, 6 evolve source using target bag stone, 7 repair source using target bag material, 8 exchange bag material in source using the recipe index in target, 9 move stored source to the chosen Muun target slot, 10 deposit bag source into the chosen Muun target slot. Operation 9 swaps occupied equipment slots and rejects occupied collection cells. Operation 10 moves displaced equipment into a free collection cell and rejects replacement when collection storage is full. Responses contain authoritative UTF-8 JSON with species catalogue and exchange recipes. Appearance packets identify the player and two visible pet types, or AttackTarget and MuunSlot for a server-authorized assisted monster attack.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   11   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x05  | Packet header - sub packet type identifier |
| 4 | 1 | Byte |  | Operation |
| 5 | 1 | Byte |  | SourceSlot |
| 6 | 1 | Byte |  | TargetSlot |
| 7 | 4 | IntegerLittleEndian |  | RequestId |