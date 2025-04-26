using System.ComponentModel.DataAnnotations;

namespace TodoList.Models
{
    public class RegisterModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
