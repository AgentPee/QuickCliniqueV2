using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentTableSchemaTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                table: "students",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordResetToken",
                table: "students",
                type: "text",
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<bool>(
                name: "IsEmailVerified",
                table: "students",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EmailVerificationTokenExpiry",
                table: "students",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmailVerificationToken",
                table: "students",
                type: "text",
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            // Add EmergencyContact columns to students table (if they don't exist)
            var addEmergencyContactName = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTNAME'
                );
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `students` ADD COLUMN `EmergencyContactName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''EmergencyContactName column already exists'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(addEmergencyContactName);

            var addEmergencyContactPhone = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTPHONENUMBER'
                );
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `students` ADD COLUMN `EmergencyContactPhoneNumber` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''EmergencyContactPhoneNumber column already exists'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(addEmergencyContactPhone);

            var addEmergencyContactRelationship = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTRELATIONSHIP'
                );
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `students` ADD COLUMN `EmergencyContactRelationship` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''EmergencyContactRelationship column already exists'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(addEmergencyContactRelationship);

            // Add columns to clinicstaff table (if they don't exist)
            var addBirthdate = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'clinicstaff' 
                    AND UPPER(COLUMN_NAME) = 'BIRTHDATE'
                );
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `clinicstaff` ADD COLUMN `Birthdate` date NULL',
                    'SELECT ''Birthdate column already exists'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(addBirthdate);

            var addGender = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'clinicstaff' 
                    AND UPPER(COLUMN_NAME) = 'GENDER'
                );
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `clinicstaff` ADD COLUMN `Gender` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''Gender column already exists'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(addGender);

            var addImage = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'clinicstaff' 
                    AND UPPER(COLUMN_NAME) = 'IMAGE'
                );
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `clinicstaff` ADD COLUMN `Image` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''Image column already exists'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(addImage);

            // Create DataProtectionKeys table (if it doesn't exist)
            var createDataProtectionKeys = @"
                SET @table_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'DataProtectionKeys'
                );
                SET @sql = IF(@table_exists = 0,
                    'CREATE TABLE `DataProtectionKeys` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `FriendlyName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
                        `Xml` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                        PRIMARY KEY (`Id`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci',
                    'SELECT ''DataProtectionKeys table already exists'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(createDataProtectionKeys);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop columns/tables only if they exist (safe rollback)
            var dropDataProtectionKeys = @"
                SET @table_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'DataProtectionKeys'
                );
                SET @sql = IF(@table_exists > 0,
                    'DROP TABLE IF EXISTS `DataProtectionKeys`',
                    'SELECT ''DataProtectionKeys table does not exist'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(dropDataProtectionKeys);

            var dropEmergencyContactName = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTNAME'
                );
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `students` DROP COLUMN `EmergencyContactName`',
                    'SELECT ''EmergencyContactName column does not exist'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(dropEmergencyContactName);

            var dropEmergencyContactPhone = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTPHONENUMBER'
                );
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `students` DROP COLUMN `EmergencyContactPhoneNumber`',
                    'SELECT ''EmergencyContactPhoneNumber column does not exist'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(dropEmergencyContactPhone);

            var dropEmergencyContactRelationship = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTRELATIONSHIP'
                );
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `students` DROP COLUMN `EmergencyContactRelationship`',
                    'SELECT ''EmergencyContactRelationship column does not exist'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(dropEmergencyContactRelationship);

            var dropBirthdate = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'clinicstaff' 
                    AND UPPER(COLUMN_NAME) = 'BIRTHDATE'
                );
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `clinicstaff` DROP COLUMN `Birthdate`',
                    'SELECT ''Birthdate column does not exist'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(dropBirthdate);

            var dropGender = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'clinicstaff' 
                    AND UPPER(COLUMN_NAME) = 'GENDER'
                );
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `clinicstaff` DROP COLUMN `Gender`',
                    'SELECT ''Gender column does not exist'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(dropGender);

            var dropImage = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'clinicstaff' 
                    AND UPPER(COLUMN_NAME) = 'IMAGE'
                );
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `clinicstaff` DROP COLUMN `Image`',
                    'SELECT ''Image column does not exist'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            migrationBuilder.Sql(dropImage);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                table: "students",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordResetToken",
                table: "students",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<bool>(
                name: "IsEmailVerified",
                table: "students",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EmailVerificationTokenExpiry",
                table: "students",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmailVerificationToken",
                table: "students",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");
        }
    }
}
