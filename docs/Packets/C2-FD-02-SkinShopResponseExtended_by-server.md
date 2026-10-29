# C2 FD 02 - SkinShopResponseExtended (by server)

## Is sent when

A skin shop catalog or purchase request was processed.

## Causes the following actions on the client side

The custom client displays UTF-8 JSON with RequestId, Status, Balance, Items (ItemType, Name, Part, Price, Eligible, Pool) and shared Pools of standard Bonuses (Number, Label). Preview changes only the local character appearance.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x02  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |