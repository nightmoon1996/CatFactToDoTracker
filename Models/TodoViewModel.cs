using System;

namespace TodoList.Models
{
    public class TodoViewModel
    {
        public int Id { get; set; }
        public required string Message { get; set; }
        public DateOnly Date { get; set; }
        public string? CatFact { get; set; }
        public string? WeatherDescription { get; set; }
    }
}
