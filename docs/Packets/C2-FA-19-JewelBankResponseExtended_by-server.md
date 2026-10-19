# C2 FA 19 - JewelBankResponseExtended (by server)

## Is sent when

A custom client opens or operates the account jewel bank.

## Causes the following actions on the client side

The server validates and commits inventory and bank changes before returning a JSON UTF-8 snapshot. Requests use monotonically increasing session identifiers; operations are 0 refresh, 1 deposit all, 2 withdraw with amount 0, 1, 10, 20 or 30.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFA  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x19  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |