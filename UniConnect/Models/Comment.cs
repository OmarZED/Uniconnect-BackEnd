using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class Comment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Content { get; set; }

        [Required]
        public string PostId { get; set; }

        [Required]
        public string AuthorId { get; set; }

        // For nested comments (replies)
        public string? ParentCommentId { get; set; }

        // Reddit-style voting system
        public int UpvoteCount { get; set; } = 0;
        public int DownvoteCount { get; set; } = 0;

        [NotMapped]
        public int Score => UpvoteCount - DownvoteCount; // Calculated score

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; }

        [ForeignKey("AuthorId")]
        public virtual ApplicationUser Author { get; set; }

        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }

        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
        public virtual ICollection<CommentVote> Votes { get; set; } = new List<CommentVote>();
    }
}