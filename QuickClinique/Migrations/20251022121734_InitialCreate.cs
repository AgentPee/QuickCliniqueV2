using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickClinique.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            // Check if schedules table exists, if not create it
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `schedules` (
                    `ScheduleID` int(100) NOT NULL AUTO_INCREMENT,
                    `Date` date NOT NULL,
                    `StartTime` time(6) NOT NULL,
                    `EndTime` time(6) NOT NULL,
                    `isAvailable` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'Yes',
                    CONSTRAINT `PRIMARY` PRIMARY KEY (`ScheduleID`)
                ) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_general_ci;
            ");

            // Alter existing column if table already exists with TEXT type
            migrationBuilder.Sql(@"
                ALTER TABLE `schedules` 
                MODIFY COLUMN `isAvailable` VARCHAR(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'Yes';
            ");

            /* Original CreateTable - commented out since we're using SQL directly to handle existing table
            migrationBuilder.CreateTable(
                name: "schedules",
                columns: table => new
                {
                    ScheduleID = table.Column<int>(type: "int(100)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(6)", maxLength: 6, nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(6)", maxLength: 6, nullable: false),
                    isAvailable = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.ScheduleID);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");
            */

            migrationBuilder.CreateTable(
                name: "usertypes",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int(100)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Role = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.UserID);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "clinicstaff",
                columns: table => new
                {
                    ClinicStaffID = table.Column<int>(type: "int(100)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserID = table.Column<int>(type: "int(100)", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEmailVerified = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    EmailVerificationToken = table.Column<string>(type: "text", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailVerificationTokenExpiry = table.Column<DateTime>(type: "datetime", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "text", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.ClinicStaffID);
                    table.ForeignKey(
                        name: "clinicstaff_ibfk_1",
                        column: x => x.UserID,
                        principalTable: "usertypes",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    MessageID = table.Column<int>(type: "int(100)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SenderID = table.Column<int>(type: "int(100)", nullable: false),
                    ReceiverID = table.Column<int>(type: "int(100)", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(6)", nullable: false, defaultValueSql: "current_timestamp(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.MessageID);
                    table.ForeignKey(
                        name: "messages_ibfk_1",
                        column: x => x.SenderID,
                        principalTable: "usertypes",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "messages_ibfk_2",
                        column: x => x.ReceiverID,
                        principalTable: "usertypes",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    StudentID = table.Column<int>(type: "int(100)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserID = table.Column<int>(type: "int(100)", nullable: false),
                    IDnumber = table.Column<int>(type: "int(100)", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEmailVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EmailVerificationToken = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailVerificationTokenExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.StudentID);
                    table.ForeignKey(
                        name: "students_ibfk_1",
                        column: x => x.UserID,
                        principalTable: "usertypes",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    AppointmentID = table.Column<int>(type: "int(100)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PatientID = table.Column<int>(type: "int(100)", nullable: false),
                    ScheduleID = table.Column<int>(type: "int(100)", nullable: false),
                    AppointmentStatus = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReasonForVisit = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symptoms = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateBooked = table.Column<DateOnly>(type: "date", nullable: false),
                    QueueNumber = table.Column<int>(type: "int(50)", nullable: false),
                    QueueStatus = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.AppointmentID);
                    table.ForeignKey(
                        name: "appointments_ibfk_1",
                        column: x => x.PatientID,
                        principalTable: "students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "appointments_ibfk_2",
                        column: x => x.ScheduleID,
                        principalTable: "schedules",
                        principalColumn: "ScheduleID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    NotificationID = table.Column<int>(type: "int(100)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClinicStaffID = table.Column<int>(type: "int(100)", nullable: false),
                    PatientID = table.Column<int>(type: "int(100)", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NotifDateTime = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false),
                    isRead = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.NotificationID);
                    table.ForeignKey(
                        name: "notification_ibfk_1",
                        column: x => x.ClinicStaffID,
                        principalTable: "clinicstaff",
                        principalColumn: "ClinicStaffID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "notification_ibfk_2",
                        column: x => x.PatientID,
                        principalTable: "students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "precords",
                columns: table => new
                {
                    RecordID = table.Column<int>(type: "int(100)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PatientID = table.Column<int>(type: "int(100)", nullable: false),
                    Diagnosis = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Medications = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Allergies = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Age = table.Column<int>(type: "int(50)", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BMI = table.Column<int>(type: "int(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.RecordID);
                    table.ForeignKey(
                        name: "precords_ibfk_1",
                        column: x => x.PatientID,
                        principalTable: "students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "history",
                columns: table => new
                {
                    HistoryID = table.Column<int>(type: "int(100)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PatientID = table.Column<int>(type: "int(100)", nullable: false),
                    AppointmentID = table.Column<int>(type: "int(100)", nullable: false),
                    ScheduleID = table.Column<int>(type: "int(100)", nullable: false),
                    VisitReason = table.Column<string>(type: "text", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IDnumber = table.Column<int>(type: "int(100)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.HistoryID);
                    table.ForeignKey(
                        name: "history_ibfk_1",
                        column: x => x.PatientID,
                        principalTable: "students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "history_ibfk_2",
                        column: x => x.AppointmentID,
                        principalTable: "appointments",
                        principalColumn: "AppointmentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "history_ibfk_3",
                        column: x => x.ScheduleID,
                        principalTable: "schedules",
                        principalColumn: "ScheduleID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "PatientID",
                table: "appointments",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "ScheduleID",
                table: "appointments",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "UserID",
                table: "clinicstaff",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "AppointmentID",
                table: "history",
                column: "AppointmentID");

            migrationBuilder.CreateIndex(
                name: "PatientID1",
                table: "history",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "ScheduleID1",
                table: "history",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "messages_ibfk_1",
                table: "messages",
                column: "SenderID");

            migrationBuilder.CreateIndex(
                name: "messages_ibfk_2",
                table: "messages",
                column: "ReceiverID");

            migrationBuilder.CreateIndex(
                name: "ClinicStaffID",
                table: "notification",
                column: "ClinicStaffID");

            migrationBuilder.CreateIndex(
                name: "PatientID2",
                table: "notification",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "PatientID3",
                table: "precords",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "UserID1",
                table: "students",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "history");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "precords");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "clinicstaff");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "schedules");

            migrationBuilder.DropTable(
                name: "usertypes");
        }
    }
}
