using System.ComponentModel.DataAnnotations;
using QuickClinique.Services;
using static QuickClinique.Services.TimeZoneHelper;

namespace QuickClinique.Models
{
    public class ForensicReportViewModel
    {
        public int StudentId { get; set; }
        
        [Required(ErrorMessage = "Patient name is required")]
        [Display(Name = "Patient Name")]
        public string PatientName { get; set; } = null!;
        
        [Display(Name = "ID Number")]
        public string? IdNumber { get; set; }
        
        [Display(Name = "Date of Birth")]
        public DateOnly? Birthdate { get; set; }
        
        [Display(Name = "Gender")]
        public string? Gender { get; set; }
        
        [Display(Name = "Report Date")]
        public DateTime ReportDate { get; set; } = GetPhilippineTime();
        
        [Display(Name = "Chief Complaint")]
        public string? ChiefComplaint { get; set; }
        
        [Display(Name = "History of Present Illness")]
        public string? HistoryOfPresentIllness { get; set; }
        
        [Display(Name = "Physical Examination Findings")]
        public string? PhysicalExamination { get; set; }
        
        [Display(Name = "Diagnosis")]
        public string? Diagnosis { get; set; }
        
        [Display(Name = "Treatment Plan")]
        public string? TreatmentPlan { get; set; }
        
        [Display(Name = "Recommendations")]
        public string? Recommendations { get; set; }
        
        [Display(Name = "Additional Notes")]
        public string? AdditionalNotes { get; set; }
        
        [Display(Name = "Attending Physician Name")]
        public string? AttendingPhysicianName { get; set; }
        
        [Display(Name = "Physician License Number")]
        public string? PhysicianLicenseNumber { get; set; }
        
        [Display(Name = "Physician Signature Date")]
        public DateTime? SignatureDate { get; set; } = GetPhilippineTime();
    }
}

