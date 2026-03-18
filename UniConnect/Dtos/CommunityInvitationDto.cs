using System.ComponentModel.DataAnnotations;
using UniConnect.Models;

namespace UniConnect.Dtos
{
    public class CommunityInvitationDto
    {
        public string Id { get; set; }
        public string CommunityId { get; set; }
        public string CommunityName { get; set; }
        public string InviterId { get; set; }
        public string InviterName { get; set; }
        public string InviteeEmail { get; set; }
        public InvitationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class CreateCommunityInvitationDto
    {
        [Required]
        [EmailAddress]
        public string InviteeEmail { get; set; }
    }
}
