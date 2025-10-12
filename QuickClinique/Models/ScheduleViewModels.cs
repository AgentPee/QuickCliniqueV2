using System.ComponentModel.DataAnnotations;

namespace QuickClinique.Models
{
    public class ScheduleBulkCreateViewModel
    {
        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        [Required(ErrorMessage = "Start time is required")]
        [Display(Name = "Start Time")]
        public TimeOnly StartTime { get; set; } = new TimeOnly(9, 0);

        [Required(ErrorMessage = "End time is required")]
        [Display(Name = "End Time")]
        public TimeOnly EndTime { get; set; } = new TimeOnly(17, 0);

        [Required(ErrorMessage = "Availability is required")]
        [Display(Name = "Availability")]
        public string IsAvailable { get; set; } = "Yes";

        [Display(Name = "Days of Week")]
        public List<string>? SelectedDays { get; set; } = new List<string>();
    }

    public class ScheduleQuickCreateViewModel
    {
        [Required(ErrorMessage = "At least one date must be selected")]
        [MinLength(1, ErrorMessage = "At least one date must be selected")]
        [Display(Name = "Select Dates")]
        public List<DateOnly> SelectedDates { get; set; } = new List<DateOnly>();

        [Required(ErrorMessage = "Start time is required")]
        [Display(Name = "Start Time")]
        public TimeOnly StartTime { get; set; } = new TimeOnly(9, 0);

        [Required(ErrorMessage = "End time is required")]
        [Display(Name = "End Time")]
        public TimeOnly EndTime { get; set; } = new TimeOnly(17, 0);

        [Required(ErrorMessage = "Availability is required")]
        [Display(Name = "Availability")]
        public string IsAvailable { get; set; } = "Yes";
    }
}