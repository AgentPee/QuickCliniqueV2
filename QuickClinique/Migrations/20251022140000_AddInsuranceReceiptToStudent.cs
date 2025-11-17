using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    public partial class AddInsuranceReceiptToStudent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add InsuranceReceipt column
            var insuranceReceiptSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'INSURANCERECEIPT'
                );
                
                SET @sql = IF(@col_exists = 0,
                    'ALTER TABLE `students` ADD COLUMN `InsuranceReceipt` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL',
                    'SELECT ''InsuranceReceipt column already exists'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(insuranceReceiptSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop InsuranceReceipt column
            var insuranceReceiptSql = @"
                SET @col_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'students' 
                    AND UPPER(COLUMN_NAME) = 'INSURANCERECEIPT'
                );
                
                SET @sql = IF(@col_exists > 0,
                    'ALTER TABLE `students` DROP COLUMN `InsuranceReceipt`',
                    'SELECT ''InsuranceReceipt column does not exist'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;";
            
            migrationBuilder.Sql(insuranceReceiptSql);
        }
    }
}

