using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnSomething.Client.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyAllListsSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StudyAllLists",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudyAllLists",
                table: "Settings");
        }
    }
}
