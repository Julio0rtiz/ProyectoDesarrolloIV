using TodoApi.Models

namespace TodoApi.Services
{
    public interface INotifierService
    {
        Task NotifyOverdueTaskAsync(TodoItem item):
    }
}