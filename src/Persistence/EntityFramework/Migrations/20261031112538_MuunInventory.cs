using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class MuunInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MuunInventoryId",
                schema: "data",
                table: "Character",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Character_MuunInventoryId",
                schema: "data",
                table: "Character",
                column: "MuunInventoryId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Character_ItemStorage_MuunInventoryId",
                schema: "data",
                table: "Character",
                column: "MuunInventoryId",
                principalSchema: "data",
                principalTable: "ItemStorage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Character_ItemStorage_MuunInventoryId",
                schema: "data",
                table: "Character");

            migrationBuilder.DropIndex(
                name: "IX_Character_MuunInventoryId",
                schema: "data",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "MuunInventoryId",
                schema: "data",
                table: "Character");
        }
    }
}
