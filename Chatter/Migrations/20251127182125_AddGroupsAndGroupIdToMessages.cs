using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chatter.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupsAndGroupIdToMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // Set GroupId = 1 for all existing messages (General group)
            migrationBuilder.Sql("UPDATE Messages SET GroupId = 1");

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeactivated = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Groups",
                columns: new[] { "Id", "CreatedAt", "Name", "OwnerId" },
                values: new object[] { 1, new DateTime(2025, 11, 27, 18, 21, 19, 41, DateTimeKind.Utc).AddTicks(5175), "General", 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_GroupId",
                table: "Messages",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OwnerId",
                table: "Groups",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Groups_GroupId",
                table: "Messages",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Groups_GroupId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Messages_GroupId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Messages");
        }
    }
}
