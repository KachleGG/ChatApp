using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chatter.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupsWithStaticDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 27, 18, 21, 19, 41, DateTimeKind.Utc).AddTicks(5175));
        }
    }
}
