using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class Credentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    HashedPassword = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Credentials",
                columns: new[] { "Id", "HashedPassword", "Username" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000000"), "AQAAAAIAAYagAAAAEKYyNm9AKgWuGR19nYSNT/7HYWJDCeC63fZKh/MfFaIaNIMhTKXzHLRXjEQ2uX6Qog==", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Credentials");
        }
    }
}
