using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormBuilderAPI.Migrations
{
    public partial class AddUsersTable_Final : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: schema already correct in DB.
            migrationBuilder.Sql("SELECT 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op
            migrationBuilder.Sql("SELECT 1;");
        }
    }
}