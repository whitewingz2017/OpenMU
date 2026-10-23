# C2 FB 21 - EventTimerResponseExtended (by server)

## Is sent when

The custom client requests its event timer.

## Causes the following actions on the client side

The client displays countdowns using the UTF-8 JSON RequestId, ServerUtc and Rows containing Name and StartsAtUtc. Dates are UTC. Only configured events on this server are included, in chronological order.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFB  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x21  | Packet header - sub packet type identifier |
| 5 |  | Binary |  | Data |