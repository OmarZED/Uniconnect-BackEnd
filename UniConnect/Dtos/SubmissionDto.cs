using System.ComponentModel.DataAnnotations;
using UniConnect.Models;

namespace UniConnect.Dtos
{
    public class SubmissionDto
    {
        public string Id { get; set; }
        public string TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public SubmissionStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
        public decimal? Grade { get; set; }
        public string? Feedback { get; set; }
        public DateTime? GradedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSubmissionDto
    {
        [Required]
        public string TaskId { get; set; }

        [Required]
        public string Content { get; set; }

        public string? AttachmentUrl { get; set; }
    }

    public class UpdateSubmissionDto
    {
        [Required]
        public string Content { get; set; }

        public string? AttachmentUrl { get; set; }
    }

    public class GradeSubmissionDto
    {
        [Required]
        [Range(0, 1000)]
        public decimal Grade { get; set; }

        public string? Feedback { get; set; }
    }
}
