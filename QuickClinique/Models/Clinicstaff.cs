using System;
using System.Collections.Generic;

namespace QuickClinique.Models;

public partial class Clinicstaff
{
    public int ClinicStaffId { get; set; }

    public int UserId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Password { get; set; } = null!;

    // Add these new properties for email verification and password reset
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

    // Professional information
    public string? LicenseNumber { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Usertype User { get; set; } = null!;
}