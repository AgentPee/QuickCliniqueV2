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

    public string PhoneNumber { get; set; } = null!; // Changed from int to string

    public string Password { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Usertype User { get; set; } = null!;
}