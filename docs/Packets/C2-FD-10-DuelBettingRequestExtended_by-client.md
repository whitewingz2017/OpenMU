# C2 FD 10 - DuelBettingRequestExtended (by client)

## Is sent when

The custom client loads the Duel Arena (0), places a bet (1), issues a public duel challenge (2), claims payouts (3) or spectates a match (4). A bet carries UTF-8 JSON with Duel and Ticket (GUIDs), Side (0 requester, 1 opponent), Currency (0 Bless, 1 Soul, 2 Life) and Amount. A challenge carries TargetId and TargetName. Spectate carries Duel (GUID).

## Causes the following actions on the server side

The server answers with the matching RequestId and this channel's public duels. A confirmed bet escrows jewels from the bettor's Jewel Bank. Claims return resolved payouts and refunds to the bank. Spectate joins an available spectator slot.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x10  | Packet header - sub packet type identifier |
| 5 | 1 | Byte |  | Operation |
| 6 | 4 | IntegerLittleEndian |  | RequestId |
| 10 |  | Binary |  | Data |