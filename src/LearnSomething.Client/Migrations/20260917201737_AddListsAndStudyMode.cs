using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnSomething.Client.Migrations
{
    /// <inheritdoc />
    public partial class AddListsAndStudyMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ListId",
                table: "Words",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveListId",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudyMode",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Lists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lists", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Words_ListId",
                table: "Words",
                column: "ListId");

            migrationBuilder.AddForeignKey(
                name: "FK_Words_Lists_ListId",
                table: "Words",
                column: "ListId",
                principalTable: "Lists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Words_Lists_ListId",
                table: "Words");

            migrationBuilder.DropTable(
                name: "Lists");

            migrationBuilder.DropIndex(
                name: "IX_Words_ListId",
                table: "Words");

            migrationBuilder.DropColumn(
                name: "ListId",
                table: "Words");

            migrationBuilder.DropColumn(
                name: "ActiveListId",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "StudyMode",
                table: "Settings");
        }
    }
}
