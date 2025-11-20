using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    /// <inheritdoc />
    public partial class AddDataProtectionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Create DataProtectionKeys table if it doesn't exist
                CREATE TABLE IF NOT EXISTS `DataProtectionKeys` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `FriendlyName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
                    `Xml` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                    CONSTRAINT `PRIMARY` PRIMARY KEY (`Id`)
                ) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_general_ci;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");
        }
    }
}

