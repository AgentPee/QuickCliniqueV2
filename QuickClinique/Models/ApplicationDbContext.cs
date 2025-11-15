using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace QuickClinique.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Clinicstaff> Clinicstaffs { get; set; }

    public virtual DbSet<History> Histories { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Precord> Precords { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Usertype> Usertypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Configuration is handled in Program.cs
        // This method is kept for backward compatibility but should not be used
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("PRIMARY");

            entity.ToTable("appointments");

            entity.HasIndex(e => e.PatientId, "PatientID");

            entity.HasIndex(e => e.ScheduleId, "ScheduleID");

            entity.Property(e => e.AppointmentId)
                .HasColumnType("int(100)")
                .HasColumnName("AppointmentID");
            entity.Property(e => e.AppointmentStatus).HasColumnType("text");
            entity.Property(e => e.PatientId)
                .HasColumnType("int(100)")
                .HasColumnName("PatientID");
            entity.Property(e => e.QueueNumber).HasColumnType("int(50)");
            entity.Property(e => e.QueueStatus).HasColumnType("text");
            entity.Property(e => e.ReasonForVisit).HasColumnType("text");
            entity.Property(e => e.ScheduleId)
                .HasColumnType("int(100)")
                .HasColumnName("ScheduleID");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("appointments_ibfk_1");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("appointments_ibfk_2");
        });

        modelBuilder.Entity<Clinicstaff>(entity =>
        {
            entity.HasKey(e => e.ClinicStaffId).HasName("PRIMARY");

            entity.ToTable("clinicstaff");

            entity.HasIndex(e => e.UserId, "UserID");

            entity.Property(e => e.ClinicStaffId)
                .HasColumnType("int(100)")
                .HasColumnName("ClinicStaffID");
            entity.Property(e => e.Email).HasColumnType("text");
            entity.Property(e => e.FirstName).HasColumnType("text");
            entity.Property(e => e.LastName).HasColumnType("text");
            entity.Property(e => e.Password).HasColumnType("text");
            entity.Property(e => e.PhoneNumber).HasColumnType("text");
            entity.Property(e => e.UserId)
                .HasColumnType("int(100)")
                .HasColumnName("UserID");

            // Add the new properties
            entity.Property(e => e.IsEmailVerified)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(false);
            entity.Property(e => e.EmailVerificationToken)
                .HasColumnType("text")
                .IsRequired(false);
            entity.Property(e => e.EmailVerificationTokenExpiry)
                .HasColumnType("datetime")
                .IsRequired(false);
            entity.Property(e => e.PasswordResetToken)
                .HasColumnType("text")
                .IsRequired(false);
            entity.Property(e => e.PasswordResetTokenExpiry)
                .HasColumnType("datetime")
                .IsRequired(false);

            // Add IsActive property configuration
            entity.Property(e => e.IsActive)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true);

            entity.HasOne(d => d.User).WithMany(p => p.Clinicstaffs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("clinicstaff_ibfk_1");
        });

        modelBuilder.Entity<History>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PRIMARY");

            entity.ToTable("history");

            entity.HasIndex(e => e.AppointmentId, "AppointmentID");

            entity.HasIndex(e => e.PatientId, "PatientID");

            entity.HasIndex(e => e.ScheduleId, "ScheduleID");

            entity.Property(e => e.HistoryId)
                .HasColumnType("int(100)")
                .HasColumnName("HistoryID");
            entity.Property(e => e.AppointmentId)
                .HasColumnType("int(100)")
                .HasColumnName("AppointmentID");
            entity.Property(e => e.Idnumber)
                .HasColumnType("int(100)")
                .HasColumnName("IDnumber");
            entity.Property(e => e.PatientId)
                .HasColumnType("int(100)")
                .HasColumnName("PatientID");
            entity.Property(e => e.ScheduleId)
                .HasColumnType("int(100)")
                .HasColumnName("ScheduleID");
            entity.Property(e => e.VisitReason).HasColumnType("text");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Histories)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("history_ibfk_2");

            entity.HasOne(d => d.Patient).WithMany(p => p.Histories)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("history_ibfk_1");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Histories)
                .HasForeignKey(d => d.ScheduleId)
                .HasConstraintName("history_ibfk_3");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PRIMARY");

            entity.ToTable("messages");

            entity.HasIndex(e => e.SenderId, "messages_ibfk_1");

            entity.HasIndex(e => e.ReceiverId, "messages_ibfk_2");

            entity.Property(e => e.MessageId)
                .HasColumnType("int(100)")
                .HasColumnName("MessageID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp(6)")
                .HasColumnType("timestamp(6)");
            entity.Property(e => e.Message1)
                .HasColumnType("text")
                .HasColumnName("Message");
            entity.Property(e => e.ReceiverId)
                .HasColumnType("int(100)")
                .HasColumnName("ReceiverID");
            entity.Property(e => e.SenderId)
                .HasColumnType("int(100)")
                .HasColumnName("SenderID");

            entity.HasOne(d => d.Receiver).WithMany(p => p.MessageReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .HasConstraintName("messages_ibfk_2");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders)
                .HasForeignKey(d => d.SenderId)
                .HasConstraintName("messages_ibfk_1");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PRIMARY");

            entity.ToTable("notification");

            entity.HasIndex(e => e.ClinicStaffId, "ClinicStaffID");

            entity.HasIndex(e => e.PatientId, "PatientID");

            entity.Property(e => e.NotificationId)
                .HasColumnType("int(100)")
                .HasColumnName("NotificationID");
            entity.Property(e => e.ClinicStaffId)
                .HasColumnType("int(100)")
                .HasColumnName("ClinicStaffID");
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.IsRead)
                .HasColumnType("text")
                .HasColumnName("isRead");
            entity.Property(e => e.NotifDateTime).HasMaxLength(6);
            entity.Property(e => e.PatientId)
                .HasColumnType("int(100)")
                .HasColumnName("PatientID");

            entity.HasOne(d => d.ClinicStaff).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ClinicStaffId)
                .HasConstraintName("notification_ibfk_1");

            entity.HasOne(d => d.Patient).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("notification_ibfk_2");
        });

        modelBuilder.Entity<Precord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PRIMARY");

            entity.ToTable("precords");

            entity.HasIndex(e => e.PatientId, "PatientID");

            entity.Property(e => e.RecordId)
                .HasColumnType("int(100)")
                .HasColumnName("RecordID");
            entity.Property(e => e.Age).HasColumnType("int(50)");
            entity.Property(e => e.Allergies).HasColumnType("text");
            entity.Property(e => e.Bmi)
                .HasColumnType("int(50)")
                .HasColumnName("BMI");
            entity.Property(e => e.Diagnosis).HasColumnType("text");
            entity.Property(e => e.Gender).HasColumnType("text");
            entity.Property(e => e.Medications).HasColumnType("text");
            entity.Property(e => e.Name).HasColumnType("text");
            entity.Property(e => e.PatientId)
                .HasColumnType("int(100)")
                .HasColumnName("PatientID");

            entity.HasOne(d => d.Patient).WithMany(p => p.Precords)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("precords_ibfk_1");
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PRIMARY");

            entity.ToTable("schedules");

            entity.Property(e => e.ScheduleId)
                .HasColumnType("int(100)")
                .HasColumnName("ScheduleID");
            entity.Property(e => e.EndTime).HasMaxLength(6);
            entity.Property(e => e.IsAvailable)
                .HasColumnType("text")
                .HasColumnName("isAvailable");
            entity.Property(e => e.StartTime).HasMaxLength(6);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PRIMARY");

            entity.ToTable("students");

            entity.HasIndex(e => e.UserId, "UserID");

            entity.Property(e => e.StudentId)
                .HasColumnType("int(100)")
                .HasColumnName("StudentID");
            entity.Property(e => e.Email).HasColumnType("text");
            entity.Property(e => e.FirstName).HasColumnType("text");
            entity.Property(e => e.Idnumber)
                .HasColumnType("int(100)")
                .HasColumnName("IDnumber");
            entity.Property(e => e.LastName).HasColumnType("text");
            entity.Property(e => e.Password).HasColumnType("text");
            entity.Property(e => e.PhoneNumber).HasColumnType("text");
            entity.Property(e => e.UserId)
                .HasColumnType("int(100)")
                .HasColumnName("UserID");

            // Add IsActive property configuration
            entity.Property(e => e.IsActive)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true);

            entity.HasOne(d => d.User).WithMany(p => p.Students)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("students_ibfk_1");
        });

        modelBuilder.Entity<Usertype>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("usertypes");

            entity.Property(e => e.UserId)
                .HasColumnType("int(100)")
                .HasColumnName("UserID");
            entity.Property(e => e.Name).HasColumnType("text");
            entity.Property(e => e.Role).HasColumnType("text");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
