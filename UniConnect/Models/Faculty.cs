using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class Faculty
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public string Description { get; set; }

        // Dean responsible for this faculty
        [Required]
        public string DeanId { get; set; }

        [ForeignKey("DeanId")]
        public virtual ApplicationUser Dean { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
        public virtual ICollection<ApplicationUser> Teachers { get; set; } = new List<ApplicationUser>();
    }
    
}

