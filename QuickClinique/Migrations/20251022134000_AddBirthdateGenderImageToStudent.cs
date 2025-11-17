using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    public partial class AddBirthdateGenderImageToStudent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Birthdate column
            var birthdateSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'BIRTHDATE'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `students` ADD COLUMN `Birthdate` date NULL',
                    'SELECT ''Birthdate column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(birthdateSql);

            // Add Gender column
            var genderSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'GENDER'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `students` ADD COLUMN `Gender` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''Gender column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(genderSql);

            // Add Image column
            var imageSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'IMAGE'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `students` ADD COLUMN `Image` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''Image column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(imageSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop Image column
            var imageSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'IMAGE'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `students` DROP COLUMN `Image`',
                    'SELECT ''Image column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(imageSql);

            // Drop Gender column
            var genderSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'GENDER'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `students` DROP COLUMN `Gender`',
                    'SELECT ''Gender column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(genderSql);

            // Drop Birthdate column
            var birthdateSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'BIRTHDATE'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `students` DROP COLUMN `Birthdate`',
                    'SELECT ''Birthdate column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(birthdateSql);
        }
    }
}

