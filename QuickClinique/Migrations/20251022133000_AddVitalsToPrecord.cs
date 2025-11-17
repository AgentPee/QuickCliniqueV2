using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    public partial class AddVitalsToPrecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add PulseRate column
            var pulseRateSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'PULSERATE'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `PulseRate` int(50) NULL',
                    'SELECT ''PulseRate column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(pulseRateSql);

            // Add BloodPressure column
            var bloodPressureSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'BLOODPRESSURE'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `BloodPressure` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''BloodPressure column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(bloodPressureSql);

            // Add Temperature column
            var temperatureSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'TEMPERATURE'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `Temperature` decimal(5,2) NULL',
                    'SELECT ''Temperature column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(temperatureSql);

            // Add RespiratoryRate column
            var respiratoryRateSql = @"
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
            
            migrationBuilder.Sql(respiratoryRateSql);

            // Add OxygenSaturation column
            var oxygenSaturationSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'OXYGENSATURATION'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `OxygenSaturation` int(50) NULL',
                    'SELECT ''OxygenSaturation column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(oxygenSaturationSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop OxygenSaturation column
            var oxygenSaturationSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'OXYGENSATURATION'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `precords` DROP COLUMN `OxygenSaturation`',
                    'SELECT ''OxygenSaturation column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(oxygenSaturationSql);

            // Drop RespiratoryRate column
            var respiratoryRateSql = @"
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
            
            migrationBuilder.Sql(respiratoryRateSql);

            // Drop Temperature column
            var temperatureSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'TEMPERATURE'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `precords` DROP COLUMN `Temperature`',
                    'SELECT ''Temperature column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(temperatureSql);

            // Drop BloodPressure column
            var bloodPressureSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'BLOODPRESSURE'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `precords` DROP COLUMN `BloodPressure`',
                    'SELECT ''BloodPressure column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(bloodPressureSql);

            // Drop PulseRate column
            var pulseRateSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'PULSERATE'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `precords` DROP COLUMN `PulseRate`',
                    'SELECT ''PulseRate column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(pulseRateSql);
        }
    }
}

