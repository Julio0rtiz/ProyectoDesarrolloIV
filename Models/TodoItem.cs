using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TodoApi.Models
{
    /*
    * Enum que representa los posibles estados de una tarea
    */
    public enum TaskState
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    /*
    * Objeto de TodoItem que representa una tarea
    */
    public class TodoItem
    {
        [Key]
        public int Id {get;set;}

        [Required]
        [MaxLength(200)]
        public string Title {get;set;} = String.Empty;

        [MaxLength(1000)]
        public string Description {get;set;} = String.Empty;

        public bool IsCompleted {get;private set;} = false;

        public DateTime CreatedAt {get;set;} = DateTime.Now;

        public DateTime? CompletedAt {get; private set;}

        public int? CategoryId {get; set;}
        public Category? Category {get; set;}

        [Required]
        public string UserId { get; set; } = String.Empty;
        public IdentityUser? User { get; set; }

        public DateTime? DueDate { get; set; }

        public bool IsOverdueNotified { get; set; } = false;

        private TaskState _state = TaskState.Pending;

        public TaskState State
        { get => _state;
            set
            {
                _state = value;
                if (_state == TaskState.Completed)
                {
                    IsCompleted = true;
                    CompletedAt = DateTime.Now;
                }
                else
                {
                    IsCompleted = false;
                    CompletedAt = null;
                }
            }
        }
    }
}