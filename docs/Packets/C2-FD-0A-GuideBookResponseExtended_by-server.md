# C2 FD 0A - GuideBookResponseExtended (by server)

## Is sent when

A Guide Book request was processed, or a game master published the guide (RequestId zero).

## Causes the following actions on the client side

The client displays UTF-8 JSON with RequestId, Status, CanEdit, Draft and Unpublished. Unless the request was refused it also contains the guide (Revision, Categories with Id and Name, Pages with Id, CategoryId, Title and Body), the server's Values which pages quote as {key}, and BossRewards (Monster, Name, WCoinC, WCoinP, GoblinPoints, CoreChance, CoreCount).

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x0A  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |