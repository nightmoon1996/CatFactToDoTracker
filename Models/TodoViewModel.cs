using System;

namespace TodoList.Models
{
    public class TodoViewModel
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string CatFact { get; set; } = string.Empty;
    }
}
