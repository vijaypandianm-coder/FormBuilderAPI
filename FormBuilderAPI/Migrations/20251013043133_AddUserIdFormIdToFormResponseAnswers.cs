using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormBuilderAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdFormIdToFormResponseAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormId",
                table: "formresponseanswers",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "formresponseanswers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_formresponseanswers_FormId",
                table: "formresponseanswers",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_formresponseanswers_UserId",
                table: "formresponseanswers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_formresponseanswers_FormId",
                table: "formresponseanswers");

            migrationBuilder.DropIndex(
                name: "IX_formresponseanswers_UserId",
                table: "formresponseanswers");

            migrationBuilder.DropColumn(
                name: "FormId",
                table: "formresponseanswers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "formresponseanswers");
        }
    }
}
