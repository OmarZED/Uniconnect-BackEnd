using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Models;

namespace UniConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager,ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="registerDto">User registration details</param>
        /// <returns>Registered user information</returns>
        /// <response code="200">User registered successfully</response>
        /// <response code="400">Invalid registration data or business rule violation</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            try
            {
                _logger.LogInformation("Registration attempt for user: {Email}", registerDto.Email);

                var result = await _authService.RegisterAsync(registerDto);

                if (result.Success)
                {
                    _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
                    return Ok(new ApiResponse<UserDto>
                    {
                        Success = true,
                        Message = result.Message,
                        Data = result.User
                    });
                }

                _logger.LogWarning("Registration failed for user {Email}: {Errors}",
                    registerDto.Email, string.Join(", ", result.Errors ?? Array.Empty<string>()));

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for user: {Email}", registerDto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred during registration",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        /// <param name="loginDto">User credentials</param>
        /// <returns>JWT token and user information</returns>
        /// <response code="200">Login successful</response>
        /// <response code="400">Invalid login data</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {Email}", loginDto.Email);

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid login data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var result = await _authService.LoginAsync(loginDto);

                if (result.Success)
                {
                    _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);

                    var loginResponse = new LoginResponseDto
                    {
                        Token = result.Token,
                        User = result.User
                        // REMOVED: TokenExpiry - frontend can decode this from the JWT token
                    };

                    return Ok(new ApiResponse<LoginResponseDto>
                    {
                        Success = true,
                        Message = result.Message,
                        Data = loginResponse
                    });
                }

                _logger.LogWarning("Login failed for user: {Email}", loginDto.Email);

                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for user: {Email}", loginDto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred during login",
                    Errors = new[] { ex.Message }
                });
            }
        }
        /// <summary>
        /// Update user profile information
        /// </summary>
        /// <param name="updateProfileDto">Updated profile data</param>
        /// <returns>Updated user information</returns>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid profile data</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto updateProfileDto)
        {
            try
            {
                // Use email from token instead of user ID
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                _logger.LogInformation("Profile update attempt for user email: {UserEmail}", userEmail);

                var result = await _authService.UpdateProfileByEmailAsync(userEmail, updateProfileDto);

                if (result.Success)
                {
                    _logger.LogInformation("Profile updated successfully for user email: {UserEmail}", userEmail);
                    return Ok(new ApiResponse<UserDto>
                    {
                        Success = true,
                        Message = result.Message,
                        Data = result.User
                    });
                }

                // Check if it's a "user not found" scenario
                if (result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    });
                }

                _logger.LogWarning("Profile update failed for user email {UserEmail}: {Errors}",
                    userEmail, string.Join(", ", result.Errors ?? Array.Empty<string>()));

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                _logger.LogError(ex, "Unexpected error during profile update for user email: {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred during profile update",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Logout the current user
        /// </summary>
        /// <returns>Logout confirmation</returns>
        /// <response code="200">Logout successful</response>
        /// <response code="500">Internal server error</response>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                _logger.LogInformation("Logout attempt for user ID: {UserId}", userId);

                var result = await _authService.LogoutAsync();

                if (result.Success)
                {
                    _logger.LogInformation("User logged out successfully: {UserId}", userId);
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = result.Message
                    });
                }

                _logger.LogWarning("Logout failed for user ID: {UserId}", userId);

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                _logger.LogError(ex, "Unexpected error during logout for user ID: {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred during logout",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get current authenticated user information
        /// </summary>
        /// <returns>Current user information</returns>
        /// <response code="200">User information retrieved successfully</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Get email from token - it's more reliable than user ID
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                _logger.LogDebug("Getting current user information for user email: {UserEmail}", userEmail);

                var user = await _authService.GetCurrentUserByEmailAsync(userEmail);
                if (user == null)
                {
                    _logger.LogWarning("User not found for email: {UserEmail}", userEmail);
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User information retrieved successfully",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                _logger.LogError(ex, "Unexpected error while getting current user for email: {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving user information",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("debug-roles")]
        public async Task<IActionResult> DebugRoles()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                UserId = userId,
                Email = userEmail,
                RoleFromToken = roleClaim,
                AllRoleClaims = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
                AllClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }
      
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPost("admin/bulk-join-communities")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> BulkJoinCommunitiesForAllStudents()
        {
            try
            {
                var students = await _userManager.Users
                    .Where(u => u.Role == UserRole.Student && u.IsActive)
                    .ToListAsync();

                int successCount = 0;
                int failCount = 0;

                foreach (var student in students)
                {
                    try
                    {
                        await _authService.HandleAutomaticCommunityJoiningAsync(student, null, null, null);
                        successCount++;
                        _logger.LogInformation("Successfully joined communities for student: {StudentId}", student.Id);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogWarning(ex, "Failed to join communities for student: {StudentId}", student.Id);
                    }
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Bulk community joining completed: {successCount} successful, {failCount} failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk community joining");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during bulk community joining",
                    Errors = new[] { ex.Message }
                });
            }
        }

        // Response wrapper for consistent API responses
        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
            public IEnumerable<string> Errors { get; set; }
        }

    }
}
