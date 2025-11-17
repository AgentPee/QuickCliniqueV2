using System;

namespace QuickClinique.Models;

public partial class Emergency
{
    public int EmergencyId { get; set; }

    public string Location { get; set; } = null!;

    public string Needs { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }
}

