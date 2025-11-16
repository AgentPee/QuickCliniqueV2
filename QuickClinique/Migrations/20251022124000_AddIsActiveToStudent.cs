using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    public partial class AddIsActiveToStudent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if column exists before adding (case-insensitive check)
            var sql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'ISACTIVE'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `students` ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT 1',
                    'SELECT ''IsActive column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Check if column exists before dropping (case-insensitive check)
            var sql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'ISACTIVE'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `students` DROP COLUMN `IsActive`',
                    'SELECT ''IsActive column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(sql);
        }
    }
}

