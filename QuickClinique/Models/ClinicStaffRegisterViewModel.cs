using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QuickClinique.Models
{
    public class ClinicStaffRegisterViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^09[0-9]{9}$", ErrorMessage = "Phone number must start with 09 and be 11 digits total")]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = null!;

        [Required]
        [Display(Name = "Birthdate")]
        [DataType(DataType.Date)]
        public DateOnly Birthdate { get; set; }

        [Required]
        [Display(Name = "Staff ID Image (Front)")]
        public IFormFile StaffIdImageFront { get; set; } = null!;

        [Required]
        [Display(Name = "Staff ID Image (Back)")]
        public IFormFile StaffIdImageBack { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}