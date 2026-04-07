using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class TaskAssignment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public DateTime DueDate { get; set; }

        public decimal MaxScore { get; set; }

        [Required]
        public string SubjectId { get; set; }

        [Required]
        public string CreatedByTeacherId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; }

        [ForeignKey("CreatedByTeacherId")]
        public virtual ApplicationUser CreatedByTeacher { get; set; }

        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}
