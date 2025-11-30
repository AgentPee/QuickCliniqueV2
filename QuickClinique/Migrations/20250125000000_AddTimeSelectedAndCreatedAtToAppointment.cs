using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    public partial class AddTimeSelectedAndCreatedAtToAppointment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TimeSelected column
            var sqlTimeSelected = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'appointments' 
                    AND UPPER(COLUMN_NAME) = 'TIMESELECTED'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `appointments` ADD COLUMN `TimeSelected` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP',
                    'SELECT ''TimeSelected column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(sqlTimeSelected);

            // Add CreatedAt column
            var sqlCreatedAt = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'appointments' 
                    AND UPPER(COLUMN_NAME) = 'CREATEDAT'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `appointments` ADD COLUMN `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP',
                    'SELECT ''CreatedAt column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(sqlCreatedAt);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop CreatedAt column
            var sqlCreatedAt = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'appointments' 
                    AND UPPER(COLUMN_NAME) = 'CREATEDAT'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `appointments` DROP COLUMN `CreatedAt`',
                    'SELECT ''CreatedAt column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(sqlCreatedAt);

            // Drop TimeSelected column
            var sqlTimeSelected = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'appointments' 
                    AND UPPER(COLUMN_NAME) = 'TIMESELECTED'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `appointments` DROP COLUMN `TimeSelected`',
                    'SELECT ''TimeSelected column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(sqlTimeSelected);
        }
    }
}

