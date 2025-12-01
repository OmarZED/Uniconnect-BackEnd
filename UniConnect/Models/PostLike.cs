using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class PostReaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string PostId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public ReactionType ReactionType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

    public enum ReactionType
    {
        Like,   // 👍
        Love,   // ❤️
        Wow,    // 😮
        Laugh,  // 😂
        Sad,    // 😢
        Angry   // 😠
    }
}