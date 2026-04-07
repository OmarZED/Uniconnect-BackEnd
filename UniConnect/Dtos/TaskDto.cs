using System.ComponentModel.DataAnnotations;

namespace UniConnect.Dtos
{
    public class TaskDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public decimal MaxScore { get; set; }
        public string SubjectId { get; set; }
        public string SubjectName { get; set; }
        public string CreatedByTeacherId { get; set; }
        public string CreatedByTeacherName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateTaskDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [Range(0.1, 1000)]
        public decimal MaxScore { get; set; }

        [Required]
        public string SubjectId { get; set; }
    }

    public class UpdateTaskDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        [Range(0.1, 1000)]
        public decimal? MaxScore { get; set; }
    }
}
