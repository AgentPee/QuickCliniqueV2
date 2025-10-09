using System;
using System.Collections.Generic;

namespace QuickClinique.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int ClinicStaffId { get; set; }

    public int PatientId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime NotifDateTime { get; set; }

    public string IsRead { get; set; } = null!;

    public virtual Clinicstaff ClinicStaff { get; set; } = null!;

    public virtual Student Patient { get; set; } = null!;
}
