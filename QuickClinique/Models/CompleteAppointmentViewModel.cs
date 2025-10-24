using System.ComponentModel.DataAnnotations;

namespace QuickClinique.Models
{
    public class CompleteAppointmentViewModel
    {
        [Required]
        public int AppointmentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Diagnosis is required")]
        [StringLength(500, ErrorMessage = "Diagnosis cannot exceed 500 characters")]
        public string Diagnosis { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Medications cannot exceed 500 characters")]
        public string? Medications { get; set; }

        [StringLength(200, ErrorMessage = "Allergies cannot exceed 200 characters")]
        public string? Allergies { get; set; }

        [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
        public int? Age { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [Range(10.0, 50.0, ErrorMessage = "BMI must be between 10 and 50")]
        public decimal? Bmi { get; set; }

        [StringLength(1000, ErrorMessage = "Additional notes cannot exceed 1000 characters")]
        public string? AdditionalNotes { get; set; }
    }
}

