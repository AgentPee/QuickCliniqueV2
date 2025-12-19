using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    /// <inheritdoc />
    public partial class CreateMedicalRecordFilesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `medicalrecordfiles` (
                    `FileID` int(100) NOT NULL AUTO_INCREMENT,
                    `PatientID` int(100) NOT NULL,
                    `RecordID` int(100) NULL,
                    `FileName` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                    `FilePath` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                    `FileType` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
                    `FileSize` bigint NOT NULL,
                    `Description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
                    `UploadedByStaffId` int(100) NULL,
                    `UploadedByName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
                    `UploadedAt` datetime NOT NULL DEFAULT current_timestamp(),
                    PRIMARY KEY (`FileID`),
                    KEY `PatientID` (`PatientID`),
                    KEY `RecordID` (`RecordID`),
                    CONSTRAINT `medicalrecordfiles_ibfk_1` FOREIGN KEY (`PatientID`) REFERENCES `students` (`StudentID`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS `medicalrecordfiles`;");
        }
    }
}


