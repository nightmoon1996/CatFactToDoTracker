using System.Collections.Generic;

namespace TodoList.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public List<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
    }
}
