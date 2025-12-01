using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class Post
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        // Post can belong to a Community OR be a personal post
        public string? CommunityId { get; set; }
        public string? AuthorId { get; set; }

        // Post visibility settings
        public PostVisibility Visibility { get; set; } = PostVisibility.Community;

        // Media attachments
        public string? ImageUrl { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }

        // Reaction counts (calculated from PostReactions)
        [NotMapped]
        public int LikeCount => Reactions.Count(r => r.ReactionType == ReactionType.Like);
        [NotMapped]
        public int LoveCount => Reactions.Count(r => r.ReactionType == ReactionType.Love);
        [NotMapped]
        public int WowCount => Reactions.Count(r => r.ReactionType == ReactionType.Wow);
        [NotMapped]
        public int LaughCount => Reactions.Count(r => r.ReactionType == ReactionType.Laugh);
        [NotMapped]
        public int SadCount => Reactions.Count(r => r.ReactionType == ReactionType.Sad);
        [NotMapped]
        public int AngryCount => Reactions.Count(r => r.ReactionType == ReactionType.Angry);

        // Total reactions for quick sorting
        public int TotalReactions { get; set; } = 0;

        // Engagement metrics
        public int CommentCount { get; set; } = 0;
        public int ShareCount { get; set; } = 0;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("CommunityId")]
        public virtual Community? Community { get; set; }

        [ForeignKey("AuthorId")]
        public virtual ApplicationUser Author { get; set; }

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    }
    public enum PostVisibility
    {
        Public,      // Visible to everyone
        Community,   // Visible only to community members (default)
        Private      // Visible only to author
    }
}

