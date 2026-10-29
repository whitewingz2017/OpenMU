using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class StoreMailCombinedPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AttachmentId",
                schema: "data",
                table: "LetterBody",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClaimRevision",
                schema: "data",
                table: "LetterBody",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "StorePricesJson",
                schema: "data",
                table: "Item",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_LetterBody_AttachmentId",
                schema: "data",
                table: "LetterBody",
                column: "AttachmentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LetterBody_Item_AttachmentId",
                schema: "data",
                table: "LetterBody",
                column: "AttachmentId",
                principalSchema: "data",
                principalTable: "Item",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LetterBody_Item_AttachmentId",
                schema: "data",
                table: "LetterBody");

            migrationBuilder.DropIndex(
                name: "IX_LetterBody_AttachmentId",
                schema: "data",
                table: "LetterBody");

            migrationBuilder.DropColumn(
                name: "AttachmentId",
                schema: "data",
                table: "LetterBody");

            migrationBuilder.DropColumn(
                name: "ClaimRevision",
                schema: "data",
                table: "LetterBody");

            migrationBuilder.DropColumn(
                name: "StorePricesJson",
                schema: "data",
                table: "Item");
        }
    }
}
