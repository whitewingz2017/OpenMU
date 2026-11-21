using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class ItemBoxRewardOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "ExcellentOptions",
                schema: "config",
                table: "ItemDropItemGroup",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasLuck",
                schema: "config",
                table: "ItemDropItemGroup",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasSkill",
                schema: "config",
                table: "ItemDropItemGroup",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "OptionLevel",
                schema: "config",
                table: "ItemDropItemGroup",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcellentOptions",
                schema: "config",
                table: "ItemDropItemGroup");

            migrationBuilder.DropColumn(
                name: "HasLuck",
                schema: "config",
                table: "ItemDropItemGroup");

            migrationBuilder.DropColumn(
                name: "HasSkill",
                schema: "config",
                table: "ItemDropItemGroup");

            migrationBuilder.DropColumn(
                name: "OptionLevel",
                schema: "config",
                table: "ItemDropItemGroup");
        }
    }
}
