using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    public partial class CreateEmergenciesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create emergencies table if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `emergencies` (
                    `EmergencyID` int(100) NOT NULL AUTO_INCREMENT,
                    `Location` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                    `Needs` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                    `CreatedAt` timestamp(6) NOT NULL DEFAULT current_timestamp(6),
                    CONSTRAINT `PRIMARY` PRIMARY KEY (`EmergencyID`)
                ) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_general_ci;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `emergencies`;");
        }
    }
}

