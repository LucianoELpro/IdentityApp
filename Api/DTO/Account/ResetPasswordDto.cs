using System.ComponentModel.DataAnnotations;

namespace Api.DTO.Account
{
    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; }
        [Required]
        [RegularExpression("^[\\w-\\.]+@[a-zA-Z_.]+?\\.[a-zA-Z]{2,3}$")]
        public string Email { get; set; }
        [Required]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "Password must be at least {2}, and maximun {1} characters")]
        public string NewPassword { get; set; }
    }
}
