using System.ComponentModel.DataAnnotations;

namespace Api.DTO.Account
{
    public class ConfirmEmailDto
    {
        [Required]
        public string Token { get; set; }
        [Required]
        [RegularExpression("^[\\w-\\.]+@[a-zA-Z_.]+?\\.[a-zA-Z]{2,3}$")]
        public string Email { get; set; }
    }
}
