using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos
{

    public class UpdateTodoItemRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = String.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = String.Empty;

        public int? CategoryId { get; set; }

        
        public DateTime? DueDate { get; set; }
    }
}
