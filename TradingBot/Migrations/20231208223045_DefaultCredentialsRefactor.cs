using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingBot.Migrations
{
    /// <inheritdoc />
    public partial class DefaultCredentialsRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Credentials",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Credentials",
                columns: new[] { "Id", "HashedPassword", "Username" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000000"), "AQAAAAIAAYagAAAAEKYyNm9AKgWuGR19nYSNT/7HYWJDCeC63fZKh/MfFaIaNIMhTKXzHLRXjEQ2uX6Qog==", "admin" });
        }
    }
}
