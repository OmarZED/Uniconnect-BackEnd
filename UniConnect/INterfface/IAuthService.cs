using UniConnect.Dtos;
using UniConnect.Models;

namespace UniConnect.INterfface
{
    public interface IAuthService
    {
        // Core Authentication Methods
        Task<AuthResult> RegisterAsync(RegisterDto registerDto);
        Task<AuthResult> LoginAsync(LoginDto loginDto);
        //Task<AuthResult> UpdateProfileAsync(string userId, UpdateProfileDto updateProfileDto);
        Task<LogoutResult> LogoutAsync();
        Task<UserDto> GetCurrentUserAsync(string userId);
        Task<UserDto> GetCurrentUserByEmailAsync(string email);
        Task<AuthResult> UpdateProfileByEmailAsync(string email, UpdateProfileDto updateProfileDto);

        // Role Checking Methods (UPDATED - No more AdminType)
        Task<bool> IsUserInRoleAsync(string userId, UserRole role);
        Task<UserRole> GetUserRoleAsync(string userId);

        // NEW: Check if user has specific academic responsibilities
        Task<bool> IsUserDeanOfFacultyAsync(string userId, string facultyId);
 
        Task<bool> IsUserInFacultyAsync(string userId, string facultyId);
        Task HandleAutomaticCommunityJoiningAsync(ApplicationUser user, string? oldFacultyId, string? oldCourseId, string? oldGroupId);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public UserDto User { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }

    public class LogoutResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}

