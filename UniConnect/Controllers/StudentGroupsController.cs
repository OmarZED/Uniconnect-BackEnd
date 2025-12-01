using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniConnect.Dtos;
using UniConnect.INterfface;
using static UniConnect.Controllers.AuthController;

namespace UniConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class StudentGroupsController : ControllerBase
    {
        private readonly IAcademicService _academicService;
        private readonly ILogger<StudentGroupsController> _logger;

        public StudentGroupsController(IAcademicService academicService, ILogger<StudentGroupsController> logger)
        {
            _academicService = academicService;
            _logger = logger;
        }

        /// <summary>
        /// Get all active student groups
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<StudentGroupDto>>), 200)]
        public async Task<IActionResult> GetAllGroups()
        {
            try
            {
                var groups = await _academicService.GetAllGroupsAsync();
                return Ok(new ApiResponse<List<StudentGroupDto>>
                {
                    Success = true,
                    Message = "Student groups retrieved successfully",
                    Data = groups
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all student groups");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving student groups",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get groups by course
        /// </summary>
        [HttpGet("course/{courseId}")]
        [ProducesResponseType(typeof(ApiResponse<List<StudentGroupDto>>), 200)]
        public async Task<IActionResult> GetGroupsByCourse(string courseId)
        {
            try
            {
                var groups = await _academicService.GetGroupsByCourseAsync(courseId);
                return Ok(new ApiResponse<List<StudentGroupDto>>
                {
                    Success = true,
                    Message = "Student groups retrieved successfully",
                    Data = groups
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups by course: {CourseId}", courseId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving student groups",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get group by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<StudentGroupDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetGroupById(string id)
        {
            try
            {
                var group = await _academicService.GetGroupByIdAsync(id);
                if (group == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Student group not found"
                    });
                }

                return Ok(new ApiResponse<StudentGroupDto>
                {
                    Success = true,
                    Message = "Student group retrieved successfully",
                    Data = group
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group by ID: {GroupId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the student group",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Create a new student group
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<StudentGroupDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> CreateGroup(CreateStudentGroupDto createGroupDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid group data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var group = await _academicService.CreateGroupAsync(createGroupDto);
                return Ok(new ApiResponse<StudentGroupDto>
                {
                    Success = true,
                    Message = "Student group created successfully",
                    Data = group
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
                _logger.LogError(ex, "Error creating student group");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating the student group",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update an existing student group
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<StudentGroupDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UpdateGroup(string id, CreateStudentGroupDto updateGroupDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid group data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var group = await _academicService.UpdateGroupAsync(id, updateGroupDto);
                return Ok(new ApiResponse<StudentGroupDto>
                {
                    Success = true,
                    Message = "Student group updated successfully",
                    Data = group
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
                _logger.LogError(ex, "Error updating group: {GroupId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating the student group",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete a student group (soft delete)
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> DeleteGroup(string id)
        {
            try
            {
                var result = await _academicService.DeleteGroupAsync(id);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Student group deleted successfully"
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
                _logger.LogError(ex, "Error deleting group: {GroupId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the student group",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get groups for dropdown selection
        /// </summary>
        [HttpGet("dropdown")]
        [ProducesResponseType(typeof(ApiResponse<List<StudentGroupDto>>), 200)]
        public async Task<IActionResult> GetGroupsForDropdown()
        {
            try
            {
                var groups = await _academicService.GetGroupsForDropdownAsync();
                return Ok(new ApiResponse<List<StudentGroupDto>>
                {
                    Success = true,
                    Message = "Groups for dropdown retrieved successfully",
                    Data = groups
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups for dropdown");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving groups for dropdown",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}

