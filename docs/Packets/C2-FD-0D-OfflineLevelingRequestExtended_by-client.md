# C2 FD 0D - OfflineLevelingRequestExtended (by client)

## Is sent when

The custom client loads the offline leveling offer (0) or starts an offline leveling session (1). A start carries UTF-8 JSON: Currency (0 zen, 1 WCoinC, 2 Goblin Points), Hours (0 pays every hour) and Protection.

## Causes the following actions on the server side

The server answers with the matching RequestId. A start which the player can pay disconnects the client and continues the character as an offline player.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x0D  | Packet header - sub packet type identifier |
| 5 | 1 | Byte |  | Operation |
| 6 | 4 | IntegerLittleEndian |  | RequestId |
| 10 |  | Binary |  | Data |