# C2 FD 0A - GuideBookRequestExtended (by client)

## Is sent when

The custom client loads the published Guide Book (0) or, as a game master, loads the draft (1), saves the draft (2) or publishes the draft (3). A save carries the whole draft as UTF-8 JSON with Revision (the draft revision it starts from), Categories (Id, Name) and Pages (Id, CategoryId, Title, Body).

## Causes the following actions on the server side

The server sends the requested guide with the matching RequestId. It refuses the draft operations to characters which aren't game masters, and a save when another game master saved since its revision. A publish copies the draft to the published guide, saves it and sends it to every player of the game server.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x0A  | Packet header - sub packet type identifier |
| 5 | 1 | Byte |  | Operation |
| 6 | 4 | IntegerLittleEndian |  | RequestId |
| 10 |  | Binary |  | Data |