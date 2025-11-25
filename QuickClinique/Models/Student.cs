using System;
using System.Collections.Generic;

namespace QuickClinique.Models;

public partial class Student
{
    public int StudentId { get; set; }
    public int UserId { get; set; }
    public int Idnumber { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;

    // Computed property for full name
    public string FullName => $"{FirstName} {LastName}";

    // Add these new properties
    public bool IsEmailVerified { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Account activation status
    public bool IsActive { get; set; } = true;

    // Personal information
    public DateOnly? Birthdate { get; set; }
    public string? Gender { get; set; }
    public string? Image { get; set; }
    public string? InsuranceReceipt { get; set; }

    // Emergency Contact Information
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhoneNumber { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<History> Histories { get; set; } = new List<History>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Precord> Precords { get; set; } = new List<Precord>();
    public virtual Usertype User { get; set; } = null!;
}
