using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseNumberToClinicstaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var licenseNumberSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'clinicstaff' 
                    AND UPPER(COLUMN_NAME) = 'LICENSENUMBER'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `clinicstaff` ADD COLUMN `LicenseNumber` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''LicenseNumber column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(licenseNumberSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'clinicstaff' 
                    AND UPPER(COLUMN_NAME) = 'LICENSENUMBER'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `clinicstaff` DROP COLUMN `LicenseNumber`',
                    'SELECT ''LicenseNumber column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }
    }
}


