# C1 FA 20 - JewelBankRequestExtended (by client)

## Is sent when

A custom client opens or operates the account jewel bank.

## Causes the following actions on the server side

The server validates and commits inventory and bank changes before returning a JSON UTF-8 snapshot. Requests use monotonically increasing session identifiers; operations are 0 refresh, 1 deposit all, 2 withdraw with amount 0, 1, 10, 20 or 30.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   15   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0xFA  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x20  | Packet header - sub packet type identifier |
| 4 | 1 | Byte |  | Operation |
| 5 | 2 | ShortLittleEndian |  | Item |
| 7 | 4 | IntegerLittleEndian |  | Amount |
| 11 | 4 | IntegerLittleEndian |  | RequestId |