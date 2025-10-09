using System;
using System.Collections.Generic;

namespace QuickClinique.Models;

public partial class History
{
    public int HistoryId { get; set; }

    public int PatientId { get; set; }

    public int AppointmentId { get; set; }

    public int ScheduleId { get; set; }

    public string VisitReason { get; set; } = null!;

    public int Idnumber { get; set; }

    public DateOnly Date { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Student Patient { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;
}
