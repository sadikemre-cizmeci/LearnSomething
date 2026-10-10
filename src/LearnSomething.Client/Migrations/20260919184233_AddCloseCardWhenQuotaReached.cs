using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnSomething.Client.Migrations
{
    /// <inheritdoc />
    public partial class AddCloseCardWhenQuotaReached : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CloseCardWhenQuotaReached",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseCardWhenQuotaReached",
                table: "Settings");
        }
    }
}
