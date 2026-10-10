using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnSomething.Client.Migrations
{
    /// <inheritdoc />
    public partial class MergeDailyWordCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReviewWordsPerDay",
                table: "Settings",
                newName: "DailyWordCount");

            // Fold the old NewWordsPerDay into the renamed column instead of
            // just discarding it, so the user's total daily activity level
            // carries over rather than silently dropping.
            migrationBuilder.Sql("UPDATE Settings SET DailyWordCount = DailyWordCount + NewWordsPerDay;");

            migrationBuilder.DropColumn(
                name: "NewWordsPerDay",
                table: "Settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DailyWordCount",
                table: "Settings",
                newName: "ReviewWordsPerDay");

            migrationBuilder.AddColumn<int>(
                name: "NewWordsPerDay",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
