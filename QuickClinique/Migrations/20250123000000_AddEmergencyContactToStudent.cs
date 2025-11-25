using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    public partial class AddEmergencyContactToStudent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if columns exist before adding (case-insensitive check)
            var sql = @"
                SET @col_exists_name = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTNAME'
                );
                
                SET @col_exists_relationship = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTRELATIONSHIP'
                );
                
                SET @col_exists_phone = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTPHONENUMBER'
                );
                
                SET @sql = IF(@col_exists_name = 0,
                    'ALTER TABLE `students` ADD COLUMN `EmergencyContactName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''EmergencyContactName column already exists'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                
                SET @sql = IF(@col_exists_relationship = 0,
                    'ALTER TABLE `students` ADD COLUMN `EmergencyContactRelationship` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''EmergencyContactRelationship column already exists'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                
                SET @sql = IF(@col_exists_phone = 0,
                    'ALTER TABLE `students` ADD COLUMN `EmergencyContactPhoneNumber` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''EmergencyContactPhoneNumber column already exists'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Check if columns exist before dropping (case-insensitive check)
            var sql = @"
                SET @col_exists_name = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTNAME'
                );
                
                SET @col_exists_relationship = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTRELATIONSHIP'
                );
                
                SET @col_exists_phone = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'EMERGENCYCONTACTPHONENUMBER'
                );
                
                SET @sql = IF(@col_exists_phone > 0,
                    'ALTER TABLE `students` DROP COLUMN `EmergencyContactPhoneNumber`',
                    'SELECT ''EmergencyContactPhoneNumber column does not exist'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                
                SET @sql = IF(@col_exists_relationship > 0,
                    'ALTER TABLE `students` DROP COLUMN `EmergencyContactRelationship`',
                    'SELECT ''EmergencyContactRelationship column does not exist'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                
                SET @sql = IF(@col_exists_name > 0,
                    'ALTER TABLE `students` DROP COLUMN `EmergencyContactName`',
                    'SELECT ''EmergencyContactName column does not exist'' AS message'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(sql);
        }
    }
}

