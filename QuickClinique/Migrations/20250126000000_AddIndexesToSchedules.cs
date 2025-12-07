using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    public partial class AddIndexesToSchedules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add index on Date column for faster date filtering
            migrationBuilder.Sql(@"
                SET @index_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'schedules' 
                    AND INDEX_NAME = 'IX_Schedules_Date'
                );
                
                SET @sql = IF(@index_exists = 0,
                    'CREATE INDEX `IX_Schedules_Date` ON `schedules` (`Date`)',
                    'SELECT ''IX_Schedules_Date index already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");

            // Add index on IsAvailable column for faster availability filtering
            migrationBuilder.Sql(@"
                SET @index_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'schedules' 
                    AND INDEX_NAME = 'IX_Schedules_IsAvailable'
                );
                
                SET @sql = IF(@index_exists = 0,
                    'CREATE INDEX `IX_Schedules_IsAvailable` ON `schedules` (`isAvailable`)',
                    'SELECT ''IX_Schedules_IsAvailable index already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");

            // Add composite index on Date and IsAvailable for combined queries
            migrationBuilder.Sql(@"
                SET @index_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'schedules' 
                    AND INDEX_NAME = 'IX_Schedules_Date_IsAvailable'
                );
                
                SET @sql = IF(@index_exists = 0,
                    'CREATE INDEX `IX_Schedules_Date_IsAvailable` ON `schedules` (`Date`, `isAvailable`)',
                    'SELECT ''IX_Schedules_Date_IsAvailable index already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");

            // Add composite index on Date and StartTime for ordering queries
            migrationBuilder.Sql(@"
                SET @index_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'schedules' 
                    AND INDEX_NAME = 'IX_Schedules_Date_StartTime'
                );
                
                SET @sql = IF(@index_exists = 0,
                    'CREATE INDEX `IX_Schedules_Date_StartTime` ON `schedules` (`Date`, `StartTime`)',
                    'SELECT ''IX_Schedules_Date_StartTime index already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes in reverse order
            migrationBuilder.Sql(@"
                SET @index_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'schedules' 
                    AND INDEX_NAME = 'IX_Schedules_Date_StartTime'
                );
                
                SET @sql = IF(@index_exists > 0,
                    'DROP INDEX `IX_Schedules_Date_StartTime` ON `schedules`',
                    'SELECT ''IX_Schedules_Date_StartTime index does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @index_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'schedules' 
                    AND INDEX_NAME = 'IX_Schedules_Date_IsAvailable'
                );
                
                SET @sql = IF(@index_exists > 0,
                    'DROP INDEX `IX_Schedules_Date_IsAvailable` ON `schedules`',
                    'SELECT ''IX_Schedules_Date_IsAvailable index does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @index_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'schedules' 
                    AND INDEX_NAME = 'IX_Schedules_IsAvailable'
                );
                
                SET @sql = IF(@index_exists > 0,
                    'DROP INDEX `IX_Schedules_IsAvailable` ON `schedules`',
                    'SELECT ''IX_Schedules_IsAvailable index does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");

            migrationBuilder.Sql(@"
                SET @index_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'schedules' 
                    AND INDEX_NAME = 'IX_Schedules_Date'
                );
                
                SET @sql = IF(@index_exists > 0,
                    'DROP INDEX `IX_Schedules_Date` ON `schedules`',
                    'SELECT ''IX_Schedules_Date index does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;");
        }
    }
}
