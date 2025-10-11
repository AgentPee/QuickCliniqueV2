using System.ComponentModel.DataAnnotations;

namespace QuickClinique.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}