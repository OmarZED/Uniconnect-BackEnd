using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Maping;
using UniConnect.Models;

namespace UniConnect.Repository
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ICommunityService _communityService;


        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            ApplicationDbContext context,
             ICommunityService communityService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _communityService = communityService;
        }

        public async Task<AuthResult> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // Validate academic fields based on role
                var validationErrors = ValidateAcademicFields(registerDto);
                if (validationErrors.Any())
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Academic field validation failed",
                        Errors = validationErrors
                    };
                }

                var user = new ApplicationUser
                {
                    UserName = registerDto.Email, // Using email as username
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    Role = registerDto.Role
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (result.Succeeded)
                {
                    // Assign to role based on UserRole
                    var roleName = registerDto.Role.ToString();
                    try
                    {
                        if (!await _roleManager.RoleExistsAsync(roleName))
                        {
                            var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));
                            if (!createRoleResult.Succeeded)
                            {
                                _logger.LogWarning("Failed to create role {Role}: {Errors}", roleName, string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                            }
                        }

                        var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
                        if (!addToRoleResult.Succeeded)
                        {
                            _logger.LogWarning("Failed to add user {User} to role {Role}: {Errors}", user.UserName, roleName, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Exception while assigning role {Role} to user {User}", roleName, user.UserName);
                        // Continue; user has been created successfully even if role assignment had issues
                    }

                    // Load academic data for the user DTO
                    var userDto = await CreateUserDtoAsync(user);

                    _logger.LogInformation("User registered successfully: {Email}", user.Email);

                    return new AuthResult
                    {
                        Success = true,
                        Message = "User created successfully",
                        User = userDto
                    };
                }

                return new AuthResult
                {
                    Success = false,
                    Message = "User creation failed",
                    Errors = result.Errors.Select(e => e.Description)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}", registerDto.Email);
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during registration",
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<AuthResult> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var result = await _signInManager.PasswordSignInAsync(
                    loginDto.Email, loginDto.Password, loginDto.RememberMe, false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(loginDto.Email);

                    // Update last login
                    user.LastLogin = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    // Generate JWT token
                    var token = GenerateJwtToken(user);

                    // Load academic data for the user DTO
                    var userDto = await CreateUserDtoAsync(user);

                    _logger.LogInformation("User logged in: {Email}", user.Email);

                    return new AuthResult
                    {
                        Success = true,
                        Message = "Login successful",
                        User = userDto,
                        Token = token
                    };
                }

                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid login attempt",
                    Errors = new[] { "Invalid email or password" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during login",
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<AuthResult> UpdateProfileByEmailAsync(string email, UpdateProfileDto updateProfileDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = new[] { "User not found" }
                    };
                }

                // === NEW: Store old academic values to check what changed ===
                var oldFacultyId = user.FacultyId;
                var oldCourseId = user.CourseId;
                var oldGroupId = user.StudentGroupId;

                // === NEW: ACADEMIC VALIDATION ===
                var validationErrors = await ValidateAcademicAssignmentAsync(updateProfileDto, user.Role);
                if (validationErrors.Any())
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Academic assignment validation failed",
                        Errors = validationErrors
                    };
                }

                // Update user properties
                user.FirstName = updateProfileDto.FirstName;
                user.LastName = updateProfileDto.LastName;
                user.ProfilePictureUrl = updateProfileDto.ProfilePictureUrl;
                user.Bio = updateProfileDto.Bio;

                // Update academic fields if provided
                if (updateProfileDto.FacultyId != null)
                    user.FacultyId = updateProfileDto.FacultyId;
                if (updateProfileDto.CourseId != null)
                    user.CourseId = updateProfileDto.CourseId;
                if (updateProfileDto.StudentGroupId != null)
                    user.StudentGroupId = updateProfileDto.StudentGroupId;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // === NEW: AUTOMATIC COMMUNITY JOINING ===
                    if (user.Role == UserRole.Student)
                    {
                        await HandleAutomaticCommunityJoiningAsync(user, oldFacultyId, oldCourseId, oldGroupId);
                    }

                    var userDto = await CreateUserDtoAsync(user);

                    _logger.LogInformation("Profile updated successfully for user: {Email}", user.Email);

                    return new AuthResult
                    {
                        Success = true,
                        Message = "Profile updated successfully",
                        User = userDto
                    };
                }

                return new AuthResult
                {
                    Success = false,
                    Message = "Profile update failed",
                    Errors = result.Errors.Select(e => e.Description)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during profile update for user {Email}", email);
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during profile update",
                    Errors = new[] { ex.Message }
                };
            }
        }
        public async Task<UserDto> GetCurrentUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            return await CreateUserDtoAsync(user);
        }

        public async Task<LogoutResult> LogoutAsync()
        {
            try
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logged out");

                return new LogoutResult
                {
                    Success = true,
                    Message = "Logout successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return new LogoutResult
                {
                    Success = false,
                    Message = "An error occurred during logout"
                };
            }
        }

        public async Task<UserDto> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return await CreateUserDtoAsync(user);
        }

        // Role Checking Methods
        public async Task<bool> IsUserInRoleAsync(string userId, UserRole role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.Role == role;
        }

        public async Task<UserRole> GetUserRoleAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.Role ?? UserRole.Student;
        }

        // Academic relationship checking methods
        public async Task<bool> IsUserDeanOfFacultyAsync(string userId, string facultyId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.Role != UserRole.Dean) return false;

            var faculty = await _context.Faculties
                .FirstOrDefaultAsync(f => f.Id == facultyId && f.DeanId == userId);
            return faculty != null;
        }

        public async Task<bool> IsUserInFacultyAsync(string userId, string facultyId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Check if user is directly assigned to faculty
            if (user.FacultyId == facultyId) return true;

            // For students, check through course
            if (user.Role == UserRole.Student && user.CourseId != null)
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == user.CourseId && c.FacultyId == facultyId);
                return course != null;
            }

            return false;
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("Role", user.Role.ToString()),
                new Claim("FacultyId", user.FacultyId ?? ""),
                new Claim("CourseId", user.CourseId ?? ""),
                new Claim("StudentGroupId", user.StudentGroupId ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<UserDto> CreateUserDtoAsync(ApplicationUser user)
        {
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Role = user.Role,
                FacultyId = user.FacultyId,
                CourseId = user.CourseId,
                StudentGroupId = user.StudentGroupId,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Bio = user.Bio,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                IsActive = user.IsActive
            };

            // Load academic names if IDs are present
            if (user.FacultyId != null)
            {
                var faculty = await _context.Faculties.FindAsync(user.FacultyId);
                userDto.FacultyName = faculty?.Name;
            }

            if (user.CourseId != null)
            {
                var course = await _context.Courses.FindAsync(user.CourseId);
                userDto.CourseName = course?.Name;
            }

            if (user.StudentGroupId != null)
            {
                var group = await _context.StudentGroups.FindAsync(user.StudentGroupId);
                userDto.StudentGroupName = group?.Name;
            }

            return userDto;
        }

        private List<string> ValidateAcademicFields(RegisterDto registerDto)
        {
            var errors = new List<string>();

            // All academic fields are optional for initial registration
            // Users can be assigned to faculties/courses/groups later by admins

            // No validation - everything is optional
            return errors;
        }

        // === NEW: Academic Validation Method ===
        private async Task<List<string>> ValidateAcademicAssignmentAsync(UpdateProfileDto updateProfileDto, UserRole userRole)
        {
            var errors = new List<string>();

            // If no academic fields are being updated, no validation needed
            if (updateProfileDto.FacultyId == null && updateProfileDto.CourseId == null && updateProfileDto.StudentGroupId == null)
            {
                return errors;
            }

            // Students must have complete academic assignment
            if (userRole == UserRole.Student)
            {
                // If assigning any academic field, all must be assigned
                if ((updateProfileDto.FacultyId != null || updateProfileDto.CourseId != null || updateProfileDto.StudentGroupId != null) &&
                    (updateProfileDto.FacultyId == null || updateProfileDto.CourseId == null || updateProfileDto.StudentGroupId == null))
                {
                    errors.Add("Students must be assigned to Faculty, Course, and Group together");
                    return errors; // Early return since further validation depends on complete data
                }

                // Validate academic hierarchy
                if (updateProfileDto.FacultyId != null && updateProfileDto.CourseId != null)
                {
                    var course = await _context.Courses
                        .FirstOrDefaultAsync(c => c.Id == updateProfileDto.CourseId && c.FacultyId == updateProfileDto.FacultyId && c.IsActive);

                    if (course == null)
                    {
                        errors.Add("Selected course does not belong to the selected faculty");
                    }
                }

                if (updateProfileDto.CourseId != null && updateProfileDto.StudentGroupId != null)
                {
                    var group = await _context.StudentGroups
                        .FirstOrDefaultAsync(g => g.Id == updateProfileDto.StudentGroupId && g.CourseId == updateProfileDto.CourseId && g.IsActive);

                    if (group == null)
                    {
                        errors.Add("Selected group does not belong to the selected course");
                    }
                }
            }

            // Teachers must be assigned to a faculty (not directly to courses)
            if (userRole == UserRole.Teacher && updateProfileDto.FacultyId == null && updateProfileDto.CourseId != null)
            {
                errors.Add("Teachers must be assigned to a faculty, not directly to courses");
            }

            // Department Managers don't need academic assignments
            if (userRole == UserRole.DepartmentManager &&
                (updateProfileDto.FacultyId != null || updateProfileDto.CourseId != null || updateProfileDto.StudentGroupId != null))
            {
                errors.Add("Department Managers should not be assigned to academic structures");
            }

            return errors;
        }
        // === NEW: Handle Automatic Community Joining ===
        public async Task HandleAutomaticCommunityJoiningAsync(ApplicationUser user, string? oldFacultyId, string? oldCourseId, string? oldGroupId)
        {
            try
            {
                _logger.LogInformation("=== STARTING AUTOMATIC COMMUNITY JOINING ===");
                _logger.LogInformation("User: {UserId}, Role: {UserRole}", user.Id, user.Role);
                _logger.LogInformation("Academic Info - Faculty: {FacultyId}, Course: {CourseId}, Group: {GroupId}",
                    user.FacultyId, user.CourseId, user.StudentGroupId);

                // If faculty changed and new faculty is set, join faculty community
                if (user.FacultyId != oldFacultyId && !string.IsNullOrEmpty(user.FacultyId))
                {
                    _logger.LogInformation("Faculty changed to {FacultyId}", user.FacultyId);
                    try
                    {
                        var facultyCommunity = await _communityService.GetOrCreateFacultyCommunityAsync(user.FacultyId);
                        _logger.LogInformation("Faculty community: {CommunityId} - {CommunityName}", facultyCommunity.Id, facultyCommunity.Name);

                        var joinResult = await _communityService.JoinCommunityAsync(facultyCommunity.Id, user.Id);
                        _logger.LogInformation("Joined faculty community: {Success}", joinResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to join faculty community for user {UserId}", user.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("Faculty not changed or empty");
                }

                // If course changed and new course is set, join course community
                if (user.CourseId != oldCourseId && !string.IsNullOrEmpty(user.CourseId))
                {
                    _logger.LogInformation("Course changed to {CourseId}", user.CourseId);
                    try
                    {
                        var courseCommunity = await _communityService.GetOrCreateCourseCommunityAsync(user.CourseId);
                        _logger.LogInformation("Course community: {CommunityId} - {CommunityName}", courseCommunity.Id, courseCommunity.Name);

                        var joinResult = await _communityService.JoinCommunityAsync(courseCommunity.Id, user.Id);
                        _logger.LogInformation("Joined course community: {Success}", joinResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to join course community for user {UserId}", user.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("Course not changed or empty");
                }

                // If group changed and new group is set, join group community
                if (user.StudentGroupId != oldGroupId && !string.IsNullOrEmpty(user.StudentGroupId))
                {
                    _logger.LogInformation("Group changed to {GroupId}", user.StudentGroupId);
                    try
                    {
                        var groupCommunity = await _communityService.GetOrCreateGroupCommunityAsync(user.StudentGroupId);
                        _logger.LogInformation("Group community: {CommunityId} - {CommunityName}", groupCommunity.Id, groupCommunity.Name);

                        var joinResult = await _communityService.JoinCommunityAsync(groupCommunity.Id, user.Id);
                        _logger.LogInformation("Joined group community: {Success}", joinResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to join group community for user {UserId}", user.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("Group not changed or empty");
                }

                _logger.LogInformation("=== COMPLETED AUTOMATIC COMMUNITY JOINING ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleAutomaticCommunityJoiningAsync for user {UserId}", user.Id);
                throw; // Re-throw to let caller handle it
            }
        }
    }
}