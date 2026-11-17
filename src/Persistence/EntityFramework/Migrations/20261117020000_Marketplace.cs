using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class Marketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttachedPriceAmount",
                schema: "data",
                table: "LetterBody",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttachedPriceType",
                schema: "data",
                table: "LetterBody",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MarketListing",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerName = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemData = table.Column<string>(type: "text", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    PriceType = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    ListedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    BuyerName = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Proceeds = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketListing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketListing_Item_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "data",
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketListing_ItemId",
                schema: "data",
                table: "MarketListing",
                column: "ItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketListing",
                schema: "data");

            migrationBuilder.DropColumn(
                name: "AttachedPriceAmount",
                schema: "data",
                table: "LetterBody");

            migrationBuilder.DropColumn(
                name: "AttachedPriceType",
                schema: "data",
                table: "LetterBody");
        }
    }
}
