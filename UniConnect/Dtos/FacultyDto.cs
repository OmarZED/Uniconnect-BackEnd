using System.ComponentModel.DataAnnotations;

namespace UniConnect.Dtos
{
    public class FacultyDto
    {
        public string Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public string Description { get; set; }
        public string DeanId { get; set; }
        public string DeanName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateFacultyDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public string Description { get; set; }
        public string DeanId { get; set; } // Optional - can assign dean later
    }

    // Course DTOs
    public class CourseDto
    {
        public string Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        public int Year { get; set; }

        [StringLength(10)]
        public string Code { get; set; }

        public string FacultyId { get; set; }
        public string FacultyName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCourseDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [Range(1, 6)]
        public int Year { get; set; }

        [StringLength(10)]
        public string Code { get; set; }

        [Required]
        public string FacultyId { get; set; }
    }

    // StudentGroup DTOs
    public class StudentGroupDto
    {
        public string Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public string Description { get; set; }
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public string FacultyId { get; set; }
        public string FacultyName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int StudentCount { get; set; }
    }

    public class CreateStudentGroupDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public string Description { get; set; }

        [Required]
        public string CourseId { get; set; }
    }
}
