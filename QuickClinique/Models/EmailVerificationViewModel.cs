using System.ComponentModel.DataAnnotations;

namespace QuickClinique.Models
{
    public class EmailVerificationViewModel
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 characters")]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}

