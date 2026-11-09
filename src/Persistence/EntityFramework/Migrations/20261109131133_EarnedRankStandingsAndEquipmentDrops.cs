using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class EarnedRankStandingsAndEquipmentDrops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "MaximumSocketCountDrop",
                schema: "config",
                table: "GameConfiguration",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DuelWins",
                schema: "data",
                table: "Character",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HideRankTitle",
                schema: "data",
                table: "Character",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDuelWinUtc",
                schema: "data",
                table: "Character",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWeeklyPlayerKillUtc",
                schema: "data",
                table: "Character",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlayerKillWeekStartUtc",
                schema: "data",
                table: "Character",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeeklyPlayerKills",
                schema: "data",
                table: "Character",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VipExpiresAtUtc",
                schema: "data",
                table: "Account",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Character_DuelWins_LastDuelWinUtc",
                schema: "data",
                table: "Character",
                columns: new[] { "DuelWins", "LastDuelWinUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Character_PlayerKillWeekStartUtc_WeeklyPlayerKills_LastWeek~",
                schema: "data",
                table: "Character",
                columns: new[] { "PlayerKillWeekStartUtc", "WeeklyPlayerKills", "LastWeeklyPlayerKillUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Character_DuelWins_LastDuelWinUtc",
                schema: "data",
                table: "Character");

            migrationBuilder.DropIndex(
                name: "IX_Character_PlayerKillWeekStartUtc_WeeklyPlayerKills_LastWeek~",
                schema: "data",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "MaximumSocketCountDrop",
                schema: "config",
                table: "GameConfiguration");

            migrationBuilder.DropColumn(
                name: "DuelWins",
                schema: "data",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "HideRankTitle",
                schema: "data",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "LastDuelWinUtc",
                schema: "data",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "LastWeeklyPlayerKillUtc",
                schema: "data",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "PlayerKillWeekStartUtc",
                schema: "data",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "WeeklyPlayerKills",
                schema: "data",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "VipExpiresAtUtc",
                schema: "data",
                table: "Account");
        }
    }
}
