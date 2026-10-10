using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnSomething.Client.Migrations
{
    /// <inheritdoc />
    public partial class MoveShowTermFirstToList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowTermFirst",
                table: "Lists",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            // Carry over whatever the user had set globally before this column moved
            // to being per-list, so existing lists keep behaving the same way.
            migrationBuilder.Sql(
                "UPDATE Lists SET ShowTermFirst = (SELECT ShowTermFirst FROM Settings WHERE Id = 1);");

            migrationBuilder.DropColumn(
                name: "ShowTermFirst",
                table: "Settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowTermFirst",
                table: "Lists");

            migrationBuilder.AddColumn<bool>(
                name: "ShowTermFirst",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
