using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudentRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns only if they don't exist (they may have been added via data seeding)
            // Using stored procedure approach for MySQL
            migrationBuilder.Sql(@"
                -- Add IsResolved column if it doesn't exist
                SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'emergencies' 
                    AND COLUMN_NAME = 'IsResolved');
                SET @sql = IF(@col_exists = 0, 
                    'ALTER TABLE `emergencies` ADD COLUMN `IsResolved` tinyint(1) NOT NULL DEFAULT 0', 
                    'SELECT ''Column IsResolved already exists'' AS message');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                -- Add StudentID column if it doesn't exist
                SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'emergencies' 
                    AND COLUMN_NAME = 'StudentID');
                SET @sql = IF(@col_exists = 0, 
                    'ALTER TABLE `emergencies` ADD COLUMN `StudentID` int(100) NULL', 
                    'SELECT ''Column StudentID already exists'' AS message');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                -- Add StudentIdNumber column if it doesn't exist
                SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'emergencies' 
                    AND COLUMN_NAME = 'StudentIdNumber');
                SET @sql = IF(@col_exists = 0, 
                    'ALTER TABLE `emergencies` ADD COLUMN `StudentIdNumber` int(100) NOT NULL DEFAULT 0', 
                    'SELECT ''Column StudentIdNumber already exists'' AS message');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                -- Add StudentName column if it doesn't exist
                SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'emergencies' 
                    AND COLUMN_NAME = 'StudentName');
                SET @sql = IF(@col_exists = 0, 
                    'ALTER TABLE `emergencies` ADD COLUMN `StudentName` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL', 
                    'SELECT ''Column StudentName already exists'' AS message');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            // Create index if it doesn't exist
            migrationBuilder.Sql(@"
                -- Create StudentID index if it doesn't exist
                SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'emergencies' 
                    AND INDEX_NAME = 'StudentID');
                SET @sql = IF(@idx_exists = 0, 
                    'CREATE INDEX `StudentID` ON `emergencies` (`StudentID`)', 
                    'SELECT ''Index StudentID already exists'' AS message');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            // Add foreign key if it doesn't exist
            migrationBuilder.Sql(@"
                -- Add foreign key if it doesn't exist
                SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'emergencies' 
                    AND CONSTRAINT_NAME = 'emergencies_ibfk_1');
                SET @sql = IF(@fk_exists = 0, 
                    'ALTER TABLE `emergencies` ADD CONSTRAINT `emergencies_ibfk_1` FOREIGN KEY (`StudentID`) REFERENCES `students` (`StudentID`)', 
                    'SELECT ''Foreign key emergencies_ibfk_1 already exists'' AS message');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "emergencies_ibfk_1",
                table: "emergencies");

            migrationBuilder.DropIndex(
                name: "StudentID",
                table: "emergencies");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "emergencies");

            migrationBuilder.DropColumn(
                name: "StudentID",
                table: "emergencies");

            migrationBuilder.DropColumn(
                name: "StudentIdNumber",
                table: "emergencies");

            migrationBuilder.DropColumn(
                name: "StudentName",
                table: "emergencies");
        }
    }
}
