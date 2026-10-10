using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnSomething.Client.Migrations
{
    /// <inheritdoc />
    public partial class MoveStudyModeToList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudyMode",
                table: "Lists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Carry over whatever the user had set globally before this column
            // moved to being per-list, so existing lists keep behaving the same way.
            migrationBuilder.Sql(
                "UPDATE Lists SET StudyMode = (SELECT StudyMode FROM Settings WHERE Id = 1);");

            migrationBuilder.DropColumn(
                name: "StudyMode",
                table: "Settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudyMode",
                table: "Lists");

            migrationBuilder.AddColumn<int>(
                name: "StudyMode",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
