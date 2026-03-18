using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class CommunityInvitation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CommunityId { get; set; }

        [Required]
        public string InviterId { get; set; }

        public string? InviteeId { get; set; }

        [Required]
        [StringLength(256)]
        public string InviteeEmail { get; set; }

        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey("CommunityId")]
        public virtual Community Community { get; set; }

        [ForeignKey("InviterId")]
        public virtual ApplicationUser Inviter { get; set; }

        [ForeignKey("InviteeId")]
        public virtual ApplicationUser? Invitee { get; set; }
    }

    public enum InvitationStatus
    {
        Pending,
        Accepted,
        Declined,
        Cancelled
    }
}
