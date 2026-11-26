# C2 FD 0C - MarketRequestExtended (by client)

## Is sent when

The custom client loads the Marketplace (0), lists an inventory item for sale (1), buys a listing (2) or cancels its own listing (3). The request carries UTF-8 JSON: Category, Search, Currency, Sort and Page for a load; Slot, PriceType and Price for a listing; Listing (the listing id) for a buy or cancel.

## Causes the following actions on the server side

The server answers with the matching RequestId. A listing moves the item out of the inventory into the market; a buy takes the payment, mails the item to the buyer and the proceeds after tax to the seller; a cancel mails the item back to the seller.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFD  | Packet header - packet type identifier |
| 4 | 1 |    Byte   | 0x0C  | Packet header - sub packet type identifier |
| 5 | 1 | Byte |  | Operation |
| 6 | 4 | IntegerLittleEndian |  | RequestId |
| 10 |  | Binary |  | Data |