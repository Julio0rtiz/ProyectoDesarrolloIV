using Microsoft.Extensions.Logging;
using TodoApi.Models;

namespace TodoApi.Services
{
    public class LoggingNotifierService : INotifierService
    {
        private readonly ILogger<LoggingNotifierService> _logger;

        public LoggingNotifierService(ILogger<LoggingNotifierService> logger)
        {
            _logger = logger;
        }

        public Task NotifyOverdueTaskAsync(TodoItem item)
        {
            _logger.LogWarning("Overdue task: ID {id} - {Title} - User: {UserID} - Due: {DueDate}",
            item.Id, item.Title, item.UserId, item.DueDate);
            return Task.CompletedTask;
        }
    }
}