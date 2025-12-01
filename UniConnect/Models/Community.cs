using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class Community
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // Community type: Faculty, Course, or Group
        [Required]
        public CommunityType Type { get; set; }

        // Links to academic structure (one of these will be set based on type)
        public string? FacultyId { get; set; }
        public string? CourseId { get; set; }
        public string? StudentGroupId { get; set; }

        // Navigation properties
        [ForeignKey("FacultyId")]
        public virtual Faculty? Faculty { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("StudentGroupId")]
        public virtual StudentGroup? StudentGroup { get; set; }

        // Community settings
        public bool IsActive { get; set; } = true;
        public bool AllowPosts { get; set; } = true;
        public bool AutoJoin { get; set; } = true; // Auto-join members based on academic assignment

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<CommunityMember> Members { get; set; } = new List<CommunityMember>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

        // Helper properties
        [NotMapped]
        public int MemberCount => Members.Count(m => m.IsActive);

        [NotMapped]
        public int PostCount => Posts.Count(p => p.IsActive);
    }

    public enum CommunityType
    {
        Faculty,    // Academic - Auto created
        Course,     // Academic - Auto created  
        Group,      // Academic - Auto created
        Department  // Administrative - Manual membership
    }
}

