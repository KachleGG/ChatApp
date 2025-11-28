using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chatter.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitesAndInviteUsages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaxUses = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    UsesCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InviteUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InviteId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceIp = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InviteUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InviteUsages_Invites_InviteId",
                        column: x => x.InviteId,
                        principalTable: "Invites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invites_Code",
                table: "Invites",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InviteUsages_InviteId",
                table: "InviteUsages",
                column: "InviteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InviteUsages");

            migrationBuilder.DropTable(
                name: "Invites");
        }
    }
}
