using System.ComponentModel.DataAnnotations;
using UniConnect.Models;

namespace UniConnect.Dtos
{
    public class PostDto
    {
        public string Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        // Community info
        public string CommunityId { get; set; }
        public string CommunityName { get; set; }

        // Author info
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string? AuthorProfilePicture { get; set; }

        // Visibility and media
        public PostVisibility Visibility { get; set; }
        public string? ImageUrl { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }

        // Engagement metrics
        public int LikeCount { get; set; }
        public int LoveCount { get; set; }
        public int WowCount { get; set; }
        public int LaughCount { get; set; }
        public int SadCount { get; set; }
        public int AngryCount { get; set; }
        public int TotalReactions { get; set; }
        public int CommentCount { get; set; }
        public int ShareCount { get; set; }

        // Current user's reaction (if any)
        public ReactionType? CurrentUserReaction { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        // Permissions
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CreatePostDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public string CommunityId { get; set; }

        public PostVisibility Visibility { get; set; } = PostVisibility.Community;

        public string? ImageUrl { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
    }

    public class UpdatePostDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Content { get; set; }

        public PostVisibility? Visibility { get; set; }

        public string? ImageUrl { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
    }
}
