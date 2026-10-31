using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class SkinInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SkinInventoryId",
                schema: "data",
                table: "Character",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Character_SkinInventoryId",
                schema: "data",
                table: "Character",
                column: "SkinInventoryId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Character_ItemStorage_SkinInventoryId",
                schema: "data",
                table: "Character",
                column: "SkinInventoryId",
                principalSchema: "data",
                principalTable: "ItemStorage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Character_ItemStorage_SkinInventoryId",
                schema: "data",
                table: "Character");

            migrationBuilder.DropIndex(
                name: "IX_Character_SkinInventoryId",
                schema: "data",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "SkinInventoryId",
                schema: "data",
                table: "Character");
        }
    }
}
