using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormBuilderAPI.Migrations
{
    /// <inheritdoc />
    public partial class Add_FormResponses_Columns_Safe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- helpers: add column if it doesn't exist
            migrationBuilder.Sql(@"
SET @exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'formresponses'
    AND COLUMN_NAME = 'FormKey');
SET @sql := IF(@exists = 0,
  'ALTER TABLE `formresponses` ADD COLUMN `FormKey` int NOT NULL DEFAULT 0',
  'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
");

            migrationBuilder.Sql(@"
SET @exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'formresponses'
    AND COLUMN_NAME = 'FieldId');
SET @sql := IF(@exists = 0,
  'ALTER TABLE `formresponses` ADD COLUMN `FieldId` varchar(255) NOT NULL DEFAULT '''' ',
  'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
");

            migrationBuilder.Sql(@"
SET @exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'formresponses'
    AND COLUMN_NAME = 'FieldType');
SET @sql := IF(@exists = 0,
  'ALTER TABLE `formresponses` ADD COLUMN `FieldType` varchar(32) NULL',
  'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
");

            migrationBuilder.Sql(@"
SET @exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'formresponses'
    AND COLUMN_NAME = 'AnswerValue');
SET @sql := IF(@exists = 0,
  'ALTER TABLE `formresponses` ADD COLUMN `AnswerValue` longtext NULL',
  'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
");

            // Make SubmittedAt default safe (only set default if column exists and no default yet)
            migrationBuilder.Sql(@"
SET @has_col := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'formresponses'
    AND COLUMN_NAME = 'SubmittedAt');
SET @has_default := (
  SELECT IF(COLUMN_DEFAULT IS NOT NULL OR EXTRA LIKE '%on update%',
            1, 0)
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'formresponses'
    AND COLUMN_NAME = 'SubmittedAt'
  LIMIT 1);
SET @sql := IF(@has_col = 1 AND IFNULL(@has_default,0) = 0,
  'ALTER TABLE `formresponses` MODIFY COLUMN `SubmittedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP',
  'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
");

            // Ensure FormId length (24) if you need it (guarded)
            migrationBuilder.Sql(@"
SET @has_col := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'formresponses'
    AND COLUMN_NAME = 'FormId');
SET @char_len := (
  SELECT CHARACTER_MAXIMUM_LENGTH
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'formresponses'
    AND COLUMN_NAME = 'FormId'
  LIMIT 1);
SET @sql := IF(@has_col = 1 AND (IFNULL(@char_len,0) <> 24),
  'ALTER TABLE `formresponses` MODIFY COLUMN `FormId` varchar(24) NOT NULL',
  'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
");

            // Optional: recreate the composite index if missing
            migrationBuilder.Sql(@"
SET @idx := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'formresponses'
    AND INDEX_NAME = 'IX_formresponses_FormId_UserId_SubmittedAt');
SET @sql := IF(@idx = 0,
  'CREATE INDEX `IX_formresponses_FormId_UserId_SubmittedAt` ON `formresponses`(`FormId`,`UserId`,`SubmittedAt`)',
  'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
");
        }
    }
}
