using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class CommentVote
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CommentId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public VoteType VoteType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CommentId")]
        public virtual Comment Comment { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

    public enum VoteType
    {
        Downvote = -1,
        Upvote = 1
    }
}
