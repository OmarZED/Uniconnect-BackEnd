using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Models;
using static UniConnect.Controllers.AuthController;

namespace UniConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SubjectsController : ControllerBase
    {
        private readonly IAcademicService _academicService;
        private readonly IAuthService _authService;
        private readonly ILogger<SubjectsController> _logger;

        public SubjectsController(IAcademicService academicService, IAuthService authService, ILogger<SubjectsController> logger)
        {
            _academicService = academicService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Create a subject (Dean or Teacher)
        /// </summary>
        [Authorize(Roles = "Dean,Teacher")]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<SubjectDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> CreateSubject(CreateSubjectDto createSubjectDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid subject data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                if (role == UserRole.Dean.ToString())
                {
                    if (string.IsNullOrWhiteSpace(createSubjectDto.StudentGroupId))
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Student group is required for dean-created subjects"
                        });
                    }

                    var group = await _academicService.GetGroupByIdAsync(createSubjectDto.StudentGroupId);
                    if (group == null)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Student group not found"
                        });
                    }

                    var canManage = await _authService.IsUserDeanOfFacultyAsync(userId, group.FacultyId);
                    if (!canManage)
                    {
                        return Forbid();
                    }
                }

                if (role == UserRole.Teacher.ToString())
                {
                    // Teacher-created subjects are standalone (no group required)
                    createSubjectDto.TeacherId = userId;
                    if (!string.IsNullOrWhiteSpace(createSubjectDto.StudentGroupId))
                    {
                        createSubjectDto.StudentGroupId = null;
                    }
                }

                var subject = await _academicService.CreateSubjectAsync(createSubjectDto);
                return Ok(new ApiResponse<SubjectDto>
                {
                    Success = true,
                    Message = "Subject created successfully",
                    Data = subject
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subject");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating the subject",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Join a subject community by join code (Student)
        /// </summary>
        [Authorize(Roles = "Student")]
        [HttpPost("join")]
        [ProducesResponseType(typeof(ApiResponse<SubjectDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> JoinSubjectByCode(JoinSubjectByCodeDto joinDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid join code",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var subject = await _academicService.JoinSubjectByCodeAsync(userId, joinDto.Code);
                return Ok(new ApiResponse<SubjectDto>
                {
                    Success = true,
                    Message = "Joined subject successfully",
                    Data = subject
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining subject by code");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while joining the subject",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get subjects by student group
        /// </summary>
        [HttpGet("group/{groupId}")]
        [ProducesResponseType(typeof(ApiResponse<List<SubjectDto>>), 200)]
        public async Task<IActionResult> GetSubjectsByGroup(string groupId)
        {
            try
            {
                var subjects = await _academicService.GetSubjectsByGroupAsync(groupId);
                return Ok(new ApiResponse<List<SubjectDto>>
                {
                    Success = true,
                    Message = "Subjects retrieved successfully",
                    Data = subjects
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subjects by group: {GroupId}", groupId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving subjects",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get subject by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<SubjectDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetSubjectById(string id)
        {
            try
            {
                var subject = await _academicService.GetSubjectByIdAsync(id);
                if (subject == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Subject not found"
                    });
                }

                return Ok(new ApiResponse<SubjectDto>
                {
                    Success = true,
                    Message = "Subject retrieved successfully",
                    Data = subject
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subject by ID: {SubjectId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the subject",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
