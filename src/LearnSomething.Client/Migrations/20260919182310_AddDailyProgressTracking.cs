using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnSomething.Client.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyProgressTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DailyProgressDate",
                table: "Lists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExceededQuotaToday",
                table: "Lists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WordsShownToday",
                table: "Lists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyProgressDate",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "ExceededQuotaToday",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "WordsShownToday",
                table: "Lists");
        }
    }
}
