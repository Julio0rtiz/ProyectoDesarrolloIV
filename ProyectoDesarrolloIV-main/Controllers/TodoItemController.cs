using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Domain;
using TodoApi.Dtos;
using System.Security.Claims;

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

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
        {
            var todoItem = await _context.TodoItems
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);
            
            if (todoItem == null) return NotFound();
            return Ok(todoItem);
        }

        [HttpPost]
        public async Task<ActionResult<TodoItem>> CreateTodoItem(TodoItem todoItem)
        {

            if (todoItem.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(t => t.Id == todoItem.CategoryId);
                if (!categoryExists) return BadRequest("The specified category does not exists");
            }

            todoItem.State = TaskState.Pending;

            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems(
            [FromQuery] TaskState? status,
            [FromQuery] bool? overdue/*completed*/,
            [FromQuery] int? categoryId)
        {
            var query = _context.TodoItems.Include(t => t.Category).AsQueryable();

            if (status/*completed*/.HasValue)
                query = query.Where(t => t.State/*IsCompleted*/ == /*completed*/status.Value);

            if (overdue == true)
            {
                var utcNow = DateTime.UtcNow;
                query = query.Where(t => t.DueDate.HasValue
                    && t.DueDate.Value < utcNow
                    && t.State != TaskState.Completed
                    && t.State != TaskState.Cancelled);
            }

            if (categoryId.HasValue)
                query = query.Where(t => t.CategoryId == categoryId.Value);

            return Ok(query);
        }

        // [HttpPost]
        // public async Task<ActionResult<TodoItem>> CreateTodoItem(string title, string description)
        // {
        //     var todoToAdd = new TodoItem();

        //     todoToAdd.Title = title;
        //     todoToAdd.Description = description;

        //     _context.TodoItems.Add(todoToAdd);
        //     await _context.SaveChangesAsync();

        //     return CreatedAtAction(nameof(GetTodoItem), new { id = todoToAdd.Id }, todoToAdd);
        // }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateTodoItem(int id, UpdateTodoItemRequest updated)
        {
            if (updated == null) return BadRequest("Updated TodoItem cannot be null");

            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null) return NotFound();

            if (updated.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(t => t.Id == updated.CategoryId);
                if (!categoryExists) return BadRequest("The specified category does not exists");
            } 

            todoItem.Title = updated.Title;
            todoItem.Description = updated.Description;
            //todoItem.State = updated.State; 
            todoItem.CategoryId = updated.CategoryId;
            if (updated.DueDate != todoItem.DueDate)   
            {
                if (updated.DueDate.HasValue
                    && (!todoItem.DueDate.HasValue || updated.DueDate.Value > todoItem.DueDate.Value))
                {
                    todoItem.IsOverdueNotified = false;
                }
                todoItem.DueDate = updated.DueDate;
            }
            
            await _context.SaveChangesAsync();
            return NoContent();
            }

        [HttpPatch("{id:int}/status")] // Cambio de toggle a status.
        public async Task<ActionResult<TodoItem>> UpdateStatus(int id, UpdateStatusRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var todoItem = await _context.TodoItems
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todoItem == null) return NotFound();

            var target = request.State!.Value;

            try
            {
                TaskStateRules.EnsureCanChange(todoItem, target, request.Force, DateTime.UtcNow);
            }
            catch (InvalidStateTransitionException ex)
            {
                return BadRequest(ex.Message);
            }

            todoItem.State = target;
            await _context.SaveChangesAsync();

            return Ok(todoItem);

            /*
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null) return NotFound();

            if (todoItem.State == TaskState.Pending)
            {
                todoItem.State = TaskState.InProgress;
            }
            else if (todoItem.State == TaskState.InProgress)
            {
                todoItem.State = TaskState.Completed;
            }
            else
            {
                return BadRequest("Cannot toggle a task that is already completed or cancelled.");
            }

            await _context.SaveChangesAsync();
            return Ok(todoItem);*/
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteTodoItem(int id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null) return NotFound();


            _context.TodoItems.Remove(todoItem);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
