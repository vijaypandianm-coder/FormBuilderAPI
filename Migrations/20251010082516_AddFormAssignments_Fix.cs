using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormBuilderAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFormAssignments_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "auditlogs");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_ActorRole",
                table: "auditlogs",
                newName: "IX_auditlogs_ActorRole");

            migrationBuilder.AddPrimaryKey(
                name: "PK_auditlogs",
                table: "auditlogs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "formassignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FormId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AssignedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formassignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_formassignments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_formassignments_FormId_UserId",
                table: "formassignments",
                columns: new[] { "FormId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_formassignments_UserId",
                table: "formassignments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "formassignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_auditlogs",
                table: "auditlogs");

            migrationBuilder.RenameTable(
                name: "auditlogs",
                newName: "AuditLogs");

            migrationBuilder.RenameIndex(
                name: "IX_auditlogs_ActorRole",
                table: "AuditLogs",
                newName: "IX_AuditLogs_ActorRole");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "Id");
        }
    }
}
