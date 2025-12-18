using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    /// <inheritdoc />
    public partial class AddTriageTrackingToPrecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TriageDateTime column
            var triageDateTimeSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'TRIAGEDATETIME'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `TriageDateTime` datetime NULL',
                    'SELECT ''TriageDateTime column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(triageDateTimeSql);

            // Add TriageTakenByStaffId column
            var triageTakenByStaffIdSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'TRIAGETAKENBYSTAFFID'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `TriageTakenByStaffId` int(100) NULL',
                    'SELECT ''TriageTakenByStaffId column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(triageTakenByStaffIdSql);

            // Add TriageTakenByName column
            var triageTakenByNameSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'TRIAGETAKENBYNAME'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `TriageTakenByName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''TriageTakenByName column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(triageTakenByNameSql);

            // Add TreatmentProvidedByStaffId column
            var treatmentProvidedByStaffIdSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'TREATMENTPROVIDEDBYSTAFFID'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `TreatmentProvidedByStaffId` int(100) NULL',
                    'SELECT ''TreatmentProvidedByStaffId column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(treatmentProvidedByStaffIdSql);

            // Add TreatmentProvidedByName column
            var treatmentProvidedByNameSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'TREATMENTPROVIDEDBYNAME'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `TreatmentProvidedByName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''TreatmentProvidedByName column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(treatmentProvidedByNameSql);

            // Add DoctorLicenseNumber column
            var doctorLicenseNumberSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'precords' 
                    AND UPPER(COLUMN_NAME) = 'DOCTORLICENSENUMBER'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `precords` ADD COLUMN `DoctorLicenseNumber` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''DoctorLicenseNumber column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(doctorLicenseNumberSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE `precords` 
                DROP COLUMN IF EXISTS `TriageDateTime`,
                DROP COLUMN IF EXISTS `TriageTakenByStaffId`,
                DROP COLUMN IF EXISTS `TriageTakenByName`,
                DROP COLUMN IF EXISTS `TreatmentProvidedByStaffId`,
                DROP COLUMN IF EXISTS `TreatmentProvidedByName`,
                DROP COLUMN IF EXISTS `DoctorLicenseNumber`;
            ");
        }
    }
}


