using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TodoApi.Data;
using TodoApi.Domain;
using TodoApi.Dtos;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoItemController : ControllerBase
    {
        private readonly TodoDBContext _context;

        public TodoItemController(TodoDBContext context)
        {
            _context = context;
        }

        // GET: api/TodoItem/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var todoItem = await _context.TodoItems
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todoItem == null)
                return NotFound();

            return Ok(todoItem);
        }

        // POST: api/TodoItem
        [HttpPost]
        public async Task<ActionResult<TodoItem>> CreateTodoItem(
            CreateTodoItemRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(t => t.Id == request.CategoryId.Value);

                if (!categoryExists)
                    return BadRequest("The specified category does not exist.");
            }

            var todoItem = new TodoItem
            {
                Title = request.Title,
                Description = request.Description,
                CategoryId = request.CategoryId,
                DueDate = request.DueDate,

                UserId = userId,

                State = TaskState.Pending
            };

            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetTodoItem),
                new { id = todoItem.Id },
                todoItem);
        }

        [HttpPost("notificar-vencidas")]
        public async Task<IActionResult> NotifyOverdueTask([FromServices] IOverdueTaskService overdueService)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            int count = await overdueService.CheckAndNotifyOverdueTaskAsync(userId);

            return Ok(new { notifiedCount = count, message = $"{count} overdue task notified successfully" });
        }

        // GET: api/TodoItem
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems(
            [FromQuery] TaskState? status,
            [FromQuery] bool? overdue,
            [FromQuery] int? categoryId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var query = _context.TodoItems
                .Include(t => t.Category)
                .Where(t => t.UserId == userId)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(t => t.State == status.Value);
            }

            if (overdue == true)
            {
                var utcNow = DateTime.UtcNow;

                query = query.Where(t =>
                    t.DueDate.HasValue
                    && t.DueDate.Value < utcNow
                    && t.State != TaskState.Completed
                    && t.State != TaskState.Cancelled);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            var todoItems = await query.ToListAsync();

            return Ok(todoItems);
        }

        // PUT: api/TodoItem/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateTodoItem(
            int id,
            UpdateTodoItemRequest updated)
        {
            if (updated == null)
                return BadRequest("Updated TodoItem cannot be null.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var todoItem = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todoItem == null)
                return NotFound();

            if (updated.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(t => t.Id == updated.CategoryId.Value);

                if (!categoryExists)
                    return BadRequest("The specified category does not exist.");
            }

            todoItem.Title = updated.Title;
            todoItem.Description = updated.Description;
            todoItem.CategoryId = updated.CategoryId;

            if (updated.DueDate != todoItem.DueDate)
            {
                if (updated.DueDate.HasValue
                    && (!todoItem.DueDate.HasValue
                        || updated.DueDate.Value > todoItem.DueDate.Value))
                {
                    todoItem.IsOverdueNotified = false;
                }

                todoItem.DueDate = updated.DueDate;
            }


            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/TodoItem/5/status
        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<TodoItem>> UpdateStatus(
            int id,
            UpdateStatusRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (request == null || !request.State.HasValue)
                return BadRequest("State is required.");

            var todoItem = await _context.TodoItems
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.UserId == userId);

            if (todoItem == null)
                return NotFound();

            var target = request.State.Value;

            try
            {
                TaskStateRules.EnsureCanChange(
                    todoItem,
                    target,
                    request.Force,
                    DateTime.UtcNow);
            }
            catch (InvalidStateTransitionException ex)
            {
                return BadRequest(ex.Message);
            }

            todoItem.State = target;

            await _context.SaveChangesAsync();

            return Ok(todoItem);
        }

        // GET: api/TodoItem/stats
        [HttpGet("stats")]
        public async Task<ActionResult> GetTodoItemStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var tasks = await _context.TodoItems
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var utcNow = DateTime.UtcNow;

            var total = tasks.Count;

            var pending = tasks.Count(t => t.State == TaskState.Pending);

            var inProgress = tasks.Count(t => t.State == TaskState.InProgress);

            var completed = tasks.Count(t => t.State == TaskState.Completed);

            var cancelled = tasks.Count(t => t.State == TaskState.Cancelled);

            var overdue = tasks.Count(t =>
                t.DueDate.HasValue
                && t.DueDate.Value < utcNow
                && t.State != TaskState.Completed
                && t.State != TaskState.Cancelled);

            var completedTasks = tasks
                .Where(t =>
                    t.State == TaskState.Completed
                    && t.CompletedAt.HasValue)
                .ToList();

            double? averageDaysToComplete = null;

            if (completedTasks.Count > 0)
            {
                averageDaysToComplete = completedTasks
                    .Average(t =>
                        (t.CompletedAt!.Value - t.CreatedAt).TotalDays);
            }

            return Ok(new
            {
                total,
                pending,
                inProgress,
                completed,
                cancelled,
                overdue,
                averageDaysToComplete
            });
        }

        // DELETE: api/TodoItem/5
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteTodoItem(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var todoItem = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todoItem == null)
                return NotFound();

            _context.TodoItems.Remove(todoItem);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}