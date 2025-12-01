using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class Course
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        public int Year { get; set; }

        [StringLength(10)]
        public string Code { get; set; }

        [Required]
        public string FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public virtual Faculty Faculty { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<StudentGroup> Groups { get; set; } = new List<StudentGroup>();

        // Helper property to get Dean through Faculty
        [NotMapped]
        public ApplicationUser Dean => Faculty.Dean;
    }
}
