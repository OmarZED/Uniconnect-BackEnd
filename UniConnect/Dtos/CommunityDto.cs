using System.ComponentModel.DataAnnotations;
using UniConnect.Models;

namespace UniConnect.Dtos
{
    public class CommunityDto
    {
        public string Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public CommunityType Type { get; set; }

        // Academic relationships (only for academic communities)
        public string? FacultyId { get; set; }
        public string? FacultyName { get; set; }

        public string? CourseId { get; set; }
        public string? CourseName { get; set; }

        public string? StudentGroupId { get; set; }
        public string? StudentGroupName { get; set; }

        // Community stats
        public int MemberCount { get; set; }
        public int PostCount { get; set; }

        // Settings
        public bool AllowPosts { get; set; }
        public bool AutoJoin { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        // Current user's role in this community (if any)
        public CommunityRole? CurrentUserRole { get; set; }
        public bool IsCurrentUserMember { get; set; }
    }

    public class CreateCommunityDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public CommunityType Type { get; set; }

        // For academic communities - link to existing academic entity
        public string? FacultyId { get; set; }
        public string? CourseId { get; set; }
        public string? StudentGroupId { get; set; }

        // Settings
        public bool AllowPosts { get; set; } = true;
        public bool AutoJoin { get; set; } = true;
    }

    public class UpdateCommunityDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool? AllowPosts { get; set; }
        public bool? AutoJoin { get; set; }
    }
    public class CreateDepartmentCommunityDto
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public bool AllowPosts { get; set; } = true;

        public bool AutoJoin { get; set; } = false;

        // NO academic fields - department communities are general
    }
}

