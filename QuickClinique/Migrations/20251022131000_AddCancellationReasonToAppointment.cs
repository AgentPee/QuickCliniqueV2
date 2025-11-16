using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    public partial class AddCancellationReasonToAppointment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if column exists before adding (case-insensitive check)
            var sql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'appointments' 
                    AND UPPER(COLUMN_NAME) = 'CANCELLATIONREASON'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `appointments` ADD COLUMN `CancellationReason` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT ''''',
                    'SELECT ''CancellationReason column already exists'' AS message'
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
                    AND TABLE_NAME = 'appointments' 
                    AND UPPER(COLUMN_NAME) = 'CANCELLATIONREASON'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `appointments` DROP COLUMN `CancellationReason`',
                    'SELECT ''CancellationReason column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(sql);
        }
    }
}

