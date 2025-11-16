using System;
using System.Collections.Generic;

namespace QuickClinique.Models;

public partial class Precord
{
    public int RecordId { get; set; }

    public int PatientId { get; set; }

    public string Diagnosis { get; set; } = null!;

    public string Medications { get; set; } = null!;

    public string Allergies { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Age { get; set; }

    public string Gender { get; set; } = null!;

    public int Bmi { get; set; }

    public string? Prescription { get; set; }

    public virtual Student Patient { get; set; } = null!;
}
