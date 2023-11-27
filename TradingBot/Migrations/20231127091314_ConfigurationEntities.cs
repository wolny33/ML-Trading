using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvestmentConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentConfiguration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestModeConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestModeConfiguration", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestmentConfiguration");

            migrationBuilder.DropTable(
                name: "TestModeConfiguration");
        }
    }
}
