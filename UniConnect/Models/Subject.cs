using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class Subject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public string? Description { get; set; }

        [Required]
        public string StudentGroupId { get; set; }

        [ForeignKey("StudentGroupId")]
        public virtual StudentGroup StudentGroup { get; set; }

        // Optional: subject may be assigned to a teacher
        public string? TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual ApplicationUser? Teacher { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
