using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Services
{
    public class OverdueTaskService : IOverdueTaskService
    {
        private readonly TodoDBContext _context;
        private readonly INotifierService _notifier;

        public OverdueTaskService(TodoDBContext context, INotifierService notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        public async Task<int> CheckAndNotifyOverdueTaskAsync(string? userId = null)
        {
            var utcNow = DateTime.UtcNow;
            var query = _context.TodoItems.Where(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value < utcNow &&
            !t.IsOverdueNotified &&
            t.State != TaskState.Completed &&
            t.State != TaskState.Cancelled);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(t => t.UserId == userId);
            }

            var overdueTasks = await query.ToListAsync();

            foreach (var task in overdueTasks)
            {
                await _notifier.NotifyOverdueTaskAsync(task);
                task.IsOverdueNotified = true;
            }

            if (overdueTasks.Any())
            {
                await _context.SaveChangesAsync();
            }
            return overdueTasks.Count;
        }
    }
}