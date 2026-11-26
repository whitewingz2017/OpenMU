# C2 FD 0B - PartySearchRequestExtended (by client)

## Is sent when

The custom client loads the Party Search (0), saves or removes the player's own listing (1) or joins a listed party (2). A save carries UTF-8 JSON with Active, MinimumLevel, GuildOnly, OnePerClass, Classes and Password; a join carries Leader and Password.

## Causes the following actions on the server side

The server answers with the matching RequestId: the result, the player's own listing and the listed parties of the game server. A join adds the player to the leader's party when the listing allows it and the password matches.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x0B  | Packet header - sub packet type identifier |
| 5 | 1 | Byte |  | Operation |
| 6 | 4 | IntegerLittleEndian |  | RequestId |
| 10 |  | Binary |  | Data |