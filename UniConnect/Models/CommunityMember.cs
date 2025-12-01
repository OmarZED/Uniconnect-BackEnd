using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class CommunityMember
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CommunityId { get; set; }

        [Required]
        public string UserId { get; set; }

        // Member role in the community
        public CommunityRole Role { get; set; } = CommunityRole.Member;

        // Membership status
        public bool IsActive { get; set; } = true;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LeftAt { get; set; }

        // Navigation properties
        [ForeignKey("CommunityId")]
        public virtual Community Community { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

    public enum CommunityRole
    {
        Member,      // Regular member
        Moderator,   // Can moderate content
        Admin        // Full community admin
    }
}

