using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnSomething.Client.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Words",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAtUtc",
                table: "Words",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NewWordsPerDay = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewWordsPerDay = table.Column<int>(type: "INTEGER", nullable: false),
                    ShowTermFirst = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartOnBoot = table.Column<bool>(type: "INTEGER", nullable: false),
                    CardX = table.Column<double>(type: "REAL", nullable: true),
                    CardY = table.Column<double>(type: "REAL", nullable: true),
                    CardWidth = table.Column<double>(type: "REAL", nullable: true),
                    CardHeight = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Words_Status",
                table: "Words",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropIndex(
                name: "IX_Words_Status",
                table: "Words");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Words");

            migrationBuilder.DropColumn(
                name: "StatusChangedAtUtc",
                table: "Words");
        }
    }
}
