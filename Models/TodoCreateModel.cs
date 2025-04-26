using System.ComponentModel.DataAnnotations;

namespace TodoList.Models
{
    public class TodoCreateModel
    {
        [Required]
        public string Message { get; set; } = string.Empty;
        [Required]
        public DateOnly Date { get; set; }
    }
}
