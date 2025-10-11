using System.ComponentModel.DataAnnotations;

namespace QuickClinique.Models
{
    public class StudentLoginViewModel
    {
        [Required]
        [Display(Name = "ID Number")]
        public int Idnumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}