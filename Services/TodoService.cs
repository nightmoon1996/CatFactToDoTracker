using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using TodoList.Data;
using TodoList.Models;

namespace TodoList.Services
{
    public class TodoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public TodoService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TodoItem?> CreateTodoAsync(TodoCreateModel model, int userId)
        {
            string catFact = await GetCatFactAsync();

            var todoItem = new TodoItem
            {
                Message = model.Message,
                Date = model.Date,
                CatFact = catFact,
                UserId = userId
            };

            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            return todoItem;
        }

        public async Task<IEnumerable<TodoViewModel>> GetTodosByUserIdAsync(int userId)
        {
            return await _context.TodoItems
                .Where(t => t.UserId == userId)
                .Select(t => new TodoViewModel
                {
                    Id = t.Id,
                    Message = t.Message,
                    Date = t.Date,
                    CatFact = t.CatFact
                })
                .ToListAsync();
        }

        private async Task<string> GetCatFactAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CatFactClient");
                var response = await client.GetAsync("fact");
                response.EnsureSuccessStatusCode();
                var catFactResponse = await response.Content.ReadFromJsonAsync<CatFactResponse>();
                return catFactResponse?.Fact ?? "Could not fetch cat fact.";
            }
            catch (HttpRequestException ex)
            {
                // Log the exception (optional)
                Console.WriteLine($"Error fetching cat fact: {ex.Message}");
                return "Could not fetch cat fact due to network error.";
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                Console.WriteLine($"Unexpected error fetching cat fact: {ex.Message}");
                return "Could not fetch cat fact due to an unexpected error.";
            }
        }
    }
}
