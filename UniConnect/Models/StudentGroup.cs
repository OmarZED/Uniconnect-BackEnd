using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class StudentGroup
    {

        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public string Description { get; set; }

        [Required]
        public string CourseId { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<ApplicationUser> Students { get; set; } = new List<ApplicationUser>();


        // Helper properties to get Faculty and Dean
        [NotMapped]
        public Faculty Faculty => Course.Faculty;

        [NotMapped]
        public ApplicationUser Dean => Course.Faculty.Dean;
    }
}
