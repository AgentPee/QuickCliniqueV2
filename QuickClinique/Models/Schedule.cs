using System;
using System.Collections.Generic;

namespace QuickClinique.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string IsAvailable { get; set; } = null!;

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<History> Histories { get; set; } = new List<History>();
}
