using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRespiratoryRateFromPrecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var removeRespiratoryRateSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'RESPIRATORYRATE'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `precords` DROP COLUMN `RespiratoryRate`',
                    'SELECT ''RespiratoryRate column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(removeRespiratoryRateSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var addRespiratoryRateSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'RESPIRATORYRATE'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `RespiratoryRate` int(50) NULL',
                    'SELECT ''RespiratoryRate column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(addRespiratoryRateSql);
        }
    }
}


