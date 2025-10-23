using System.ComponentModel.DataAnnotations;

namespace QuickClinique.Models
{
    public class AppointmentCreateViewModel
    {
        [Required(ErrorMessage = "Patient ID is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Schedule ID is required")]
        public int ScheduleId { get; set; }

        [Required(ErrorMessage = "Please select a service for your appointment")]
        [Display(Name = "Service Required")]
        public string ReasonForVisit { get; set; } = string.Empty;

        [Display(Name = "Symptoms/Concerns")]
        public string Symptoms { get; set; } = string.Empty;
    }
}
