using System.ComponentModel.DataAnnotations;
using TodoApi.Models;

namespace TodoApi.Dtos
{

    public class UpdateStatusRequest
    {
        [Required]
        public TaskState? State { get; set; }

        public bool Force { get; set; } = false;
    }
}
