using System.ComponentModel.DataAnnotations;
using UniConnect.Models;

namespace UniConnect.Dtos
{
    public class CommentDto
    {
        public string Id { get; set; }

        [Required]
        public string Content { get; set; }

        public string PostId { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string? AuthorProfilePicture { get; set; }

        // For nested comments
        public string? ParentCommentId { get; set; }
        public List<CommentDto> Replies { get; set; } = new List<CommentDto>();

        // Reddit-style voting
        public int UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public int Score { get; set; }

        // Current user's vote (if any)
        public VoteType? CurrentUserVote { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        // Permissions
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CreateCommentDto
    {
        [Required]
        public string Content { get; set; }

        [Required]
        public string PostId { get; set; }

        public string? ParentCommentId { get; set; } // For replies
    }

    public class UpdateCommentDto
    {
        [Required]
        public string Content { get; set; }
    }
}
