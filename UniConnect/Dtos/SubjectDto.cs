using System.ComponentModel.DataAnnotations;

namespace UniConnect.Dtos
{
    public class SubjectDto
    {
        public string Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public string? Description { get; set; }

        public string StudentGroupId { get; set; }
        public string StudentGroupName { get; set; }
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public string FacultyId { get; set; }
        public string FacultyName { get; set; }

        public string? TeacherId { get; set; }
        public string? TeacherName { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSubjectDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public string? Description { get; set; }

        [Required]
        public string StudentGroupId { get; set; }

        // Optional: dean can assign a teacher, teacher will be forced to self
        public string? TeacherId { get; set; }
    }
}
