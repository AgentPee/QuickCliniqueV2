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

    public string Symptoms { get; set; } = string.Empty; // New column for symptom data

    public string TriageNotes { get; set; } = string.Empty; // Triage notes from clinical assessment

    public string CancellationReason { get; set; } = string.Empty; // Reason for appointment cancellation

    public DateOnly DateBooked { get; set; }

    public int QueueNumber { get; set; }

    public string QueueStatus { get; set; } = null!;

    public virtual ICollection<History> Histories { get; set; } = new List<History>();

    public virtual Student Patient { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;
}