using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniConnect.Models
{
    public class ApplicationUser : IdentityUser
    {

        [Required]
        [PersonalData]
        public string FirstName { get; set; }

        [Required]
        [PersonalData]
        public string LastName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;

        public UserRole Role { get; set; }

        // === ACADEMIC RELATIONSHIPS ===

        // For Students: Link to their academic group
        public string? StudentGroupId { get; set; }
        [ForeignKey("StudentGroupId")]
        public virtual StudentGroup? StudentGroup { get; set; }

        // For Students: Direct link to course (for better performance)
        public string? CourseId { get; set; }
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        // For Teachers & Deans: Link to faculty
        public string? FacultyId { get; set; }
        [ForeignKey("FacultyId")]
        public virtual Faculty? Faculty { get; set; }

        


        // For Deans: Faculties they manage (if multiple)
        public virtual ICollection<Faculty> ManagedFaculties { get; set; } = new List<Faculty>();

        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }

    public enum UserRole
    {
        Student,
        Teacher,
        Dean,
        DepartmentManager
    }
}


