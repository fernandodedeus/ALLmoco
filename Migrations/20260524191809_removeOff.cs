using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ALLmoco.Migrations
{
    /// <inheritdoc />
    public partial class removeOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MealOff",
                table: "MealChecks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MealOff",
                table: "MealChecks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
