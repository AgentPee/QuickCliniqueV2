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

    public int? PulseRate { get; set; }

    public string? BloodPressure { get; set; }

    public decimal? Temperature { get; set; }

    public int? OxygenSaturation { get; set; }

    // Triage tracking information
    public DateTime? TriageDateTime { get; set; } // Date and time when triage was taken
    public int? TriageTakenByStaffId { get; set; } // ID of staff member who took the vital signs
    public string? TriageTakenByName { get; set; } // Name of staff member who took the vital signs
    public int? TreatmentProvidedByStaffId { get; set; } // ID of doctor/staff who provided treatment
    public string? TreatmentProvidedByName { get; set; } // Name of doctor/staff who provided treatment
    public string? DoctorLicenseNumber { get; set; } // License number of the doctor providing treatment

    public virtual Student Patient { get; set; } = null!;
}
