using System.ComponentModel.DataAnnotations;
using UniConnect.Models;

namespace UniConnect.Dtos
{
    public class CommunityMemberDto
    {
        public string Id { get; set; }
        public string CommunityId { get; set; }
        public string UserId { get; set; }

        // User details
        public string UserEmail { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string UserFullName { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Member info
        public CommunityRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateMemberRoleDto
    {
        [Required]
        public CommunityRole Role { get; set; }
    }
}
