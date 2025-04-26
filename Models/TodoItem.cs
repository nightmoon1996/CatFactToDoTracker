namespace TodoList.Models
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string CatFact { get; set; } = string.Empty;
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
