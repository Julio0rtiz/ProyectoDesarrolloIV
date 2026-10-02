using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos
{
    public class CreateTodoItemRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        public DateTime? DueDate { get; set; }
    }
}