using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class Submission
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string TaskId { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public string Content { get; set; }

        public string? AttachmentUrl { get; set; }

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public decimal? Grade { get; set; }
        public string? Feedback { get; set; }
        public DateTime? GradedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey("TaskId")]
        public virtual TaskAssignment Task { get; set; }

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; }
    }

    public enum SubmissionStatus
    {
        Draft,
        Submitted,
        Graded
    }
}
