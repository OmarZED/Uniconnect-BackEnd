using System.ComponentModel.DataAnnotations;
using UniConnect.Models;

namespace UniConnect.Dtos
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public UserRole Role { get; set; }

    }

    public class LoginDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
        public bool RememberMe { get; set; } = false;
    }
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public UserDto User { get; set; }

    }
    public class UpdateProfileDto
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }

        // Academic updates (optional)
        public string? FacultyId { get; set; }
        public string? CourseId { get; set; }
        public string? StudentGroupId { get; set; }
    }
    public class UserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }

        // REMOVED: public AdminType? AdminType { get; set; }

        // NEW: Academic information
        public string? FacultyId { get; set; }
        public string? FacultyName { get; set; }
        public string? CourseId { get; set; }
        public string? CourseName { get; set; }
        public string? StudentGroupId { get; set; }
        public string? StudentGroupName { get; set; }

        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
    }
}
