using System;

namespace QuickClinique.Models;

public partial class Emergency
{
    public int EmergencyId { get; set; }

    public int? StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public int StudentIdNumber { get; set; }

    public string Location { get; set; } = null!;

    public string Needs { get; set; } = null!;

    public bool IsResolved { get; set; } = false;

    public DateTime? CreatedAt { get; set; }

    public virtual Student? Student { get; set; }
}

