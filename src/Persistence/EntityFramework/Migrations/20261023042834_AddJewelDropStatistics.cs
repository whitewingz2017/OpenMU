using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddJewelDropStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JewelDropTotal",
                schema: "data",
                columns: table => new
                {
                    ItemCode = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<long>(type: "bigint", nullable: false),
                    TrackingSinceUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JewelDropTotal", x => x.ItemCode);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JewelDropTotal",
                schema: "data");
        }
    }
}
