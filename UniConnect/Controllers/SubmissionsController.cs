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
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;
        private readonly ILogger<SubmissionsController> _logger;

        public SubmissionsController(ISubmissionService submissionService, ILogger<SubmissionsController> logger)
        {
            _submissionService = submissionService;
            _logger = logger;
        }

        /// <summary>
        /// Create a submission for a task (Student only). Single submission per task; edits allowed until graded.
        /// </summary>
        [Authorize(Roles = "Student")]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<SubmissionDto>), 200)]
        public async Task<IActionResult> CreateSubmission(CreateSubmissionDto createSubmissionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid submission data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(studentId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var submission = await _submissionService.CreateSubmissionAsync(createSubmissionDto, studentId);
                return Ok(new ApiResponse<SubmissionDto>
                {
                    Success = true,
                    Message = "Submission created successfully",
                    Data = submission
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
                _logger.LogError(ex, "Error creating submission");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating the submission",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get all submissions for a task (Teacher only).
        /// </summary>
        [Authorize(Roles = "Teacher")]
        [HttpGet("/api/tasks/{taskId}/submissions")]
        [ProducesResponseType(typeof(ApiResponse<List<SubmissionDto>>), 200)]
        public async Task<IActionResult> GetSubmissionsByTask(string taskId)
        {
            try
            {
                var teacherId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(teacherId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var submissions = await _submissionService.GetSubmissionsByTaskAsync(taskId, teacherId);
                return Ok(new ApiResponse<List<SubmissionDto>>
                {
                    Success = true,
                    Message = "Submissions retrieved successfully",
                    Data = submissions
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
                _logger.LogError(ex, "Error getting submissions for task: {TaskId}", taskId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving submissions",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get a submission by ID (Teacher for owned subject or owner Student).
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<SubmissionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetSubmissionById(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var isTeacher = string.Equals(role, UserRole.Teacher.ToString(), StringComparison.OrdinalIgnoreCase);

                var submission = await _submissionService.GetSubmissionByIdAsync(id, userId, isTeacher);
                if (submission == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Submission not found"
                    });
                }

                return Ok(new ApiResponse<SubmissionDto>
                {
                    Success = true,
                    Message = "Submission retrieved successfully",
                    Data = submission
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission by ID: {SubmissionId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the submission",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update a submission (Student only). Not allowed after grading.
        /// </summary>
        [Authorize(Roles = "Student")]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<SubmissionDto>), 200)]
        public async Task<IActionResult> UpdateSubmission(string id, UpdateSubmissionDto updateSubmissionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid submission data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(studentId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var submission = await _submissionService.UpdateSubmissionAsync(id, updateSubmissionDto, studentId);
                return Ok(new ApiResponse<SubmissionDto>
                {
                    Success = true,
                    Message = "Submission updated successfully",
                    Data = submission
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating submission: {SubmissionId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating the submission",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Grade a submission (Teacher only for owned subject).
        /// </summary>
        [Authorize(Roles = "Teacher")]
        [HttpPut("{id}/grade")]
        [ProducesResponseType(typeof(ApiResponse<SubmissionDto>), 200)]
        public async Task<IActionResult> GradeSubmission(string id, GradeSubmissionDto gradeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid grade data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var teacherId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(teacherId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var submission = await _submissionService.GradeSubmissionAsync(id, gradeDto, teacherId);
                return Ok(new ApiResponse<SubmissionDto>
                {
                    Success = true,
                    Message = "Submission graded successfully",
                    Data = submission
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error grading submission: {SubmissionId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while grading the submission",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
