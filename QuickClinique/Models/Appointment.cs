using System;
using System.Collections.Generic;

namespace QuickClinique.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int ScheduleId { get; set; }

    public string AppointmentStatus { get; set; } = null!;

    public string ReasonForVisit { get; set; } = null!;

    public string Symptoms { get; set; } = null!; // New column for symptom data

    public DateOnly DateBooked { get; set; }

    public int QueueNumber { get; set; }

    public string QueueStatus { get; set; } = null!;

    public virtual ICollection<History> Histories { get; set; } = new List<History>();

    public virtual Student Patient { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;
}