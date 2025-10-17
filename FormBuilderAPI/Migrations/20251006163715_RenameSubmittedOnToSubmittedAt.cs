using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormBuilderAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenameSubmittedOnToSubmittedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "FormResponses");

            migrationBuilder.DropColumn(
                name: "AnswerText",
                table: "FormResponseAnswers");

            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "FormResponseAnswers");

            migrationBuilder.RenameColumn(
                name: "SubmittedOn",
                table: "FormResponses",
                newName: "SubmittedAt");

            migrationBuilder.RenameColumn(
                name: "ResponseId",
                table: "FormResponses",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_FormResponses_FormId_SubmittedOn",
                table: "FormResponses",
                newName: "IX_FormResponses_FormId_SubmittedAt");

            migrationBuilder.AlterColumn<long>(
                name: "FormId",
                table: "FormResponses",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "FormResponses",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AnswerValue",
                table: "FormResponseAnswers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FieldId",
                table: "FormResponseAnswers",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ActorRole",
                table: "AuditLogs",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponseAnswers_FieldId",
                table: "FormResponseAnswers",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorRole",
                table: "AuditLogs",
                column: "ActorRole");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FormResponseAnswers_FieldId",
                table: "FormResponseAnswers");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActorRole",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FormResponses");

            migrationBuilder.DropColumn(
                name: "AnswerValue",
                table: "FormResponseAnswers");

            migrationBuilder.DropColumn(
                name: "FieldId",
                table: "FormResponseAnswers");

            migrationBuilder.RenameColumn(
                name: "SubmittedAt",
                table: "FormResponses",
                newName: "SubmittedOn");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "FormResponses",
                newName: "ResponseId");

            migrationBuilder.RenameIndex(
                name: "IX_FormResponses_FormId_SubmittedAt",
                table: "FormResponses",
                newName: "IX_FormResponses_FormId_SubmittedOn");

            migrationBuilder.AlterColumn<string>(
                name: "FormId",
                table: "FormResponses",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByUserId",
                table: "FormResponses",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AnswerText",
                table: "FormResponseAnswers",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "QuestionId",
                table: "FormResponseAnswers",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ActorRole",
                table: "AuditLogs",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
