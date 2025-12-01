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
    public class CoursesController : ControllerBase
    {
        private readonly IAcademicService _academicService;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(IAcademicService academicService, ILogger<CoursesController> logger)
        {
            _academicService = academicService;
            _logger = logger;
        }

        /// <summary>
        /// Get all active courses
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CourseDto>>), 200)]
        public async Task<IActionResult> GetAllCourses()
        {
            try
            {
                var courses = await _academicService.GetAllCoursesAsync();
                return Ok(new ApiResponse<List<CourseDto>>
                {
                    Success = true,
                    Message = "Courses retrieved successfully",
                    Data = courses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all courses");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving courses",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get courses by faculty
        /// </summary>
        [HttpGet("faculty/{facultyId}")]
        [ProducesResponseType(typeof(ApiResponse<List<CourseDto>>), 200)]
        public async Task<IActionResult> GetCoursesByFaculty(string facultyId)
        {
            try
            {
                var courses = await _academicService.GetCoursesByFacultyAsync(facultyId);
                return Ok(new ApiResponse<List<CourseDto>>
                {
                    Success = true,
                    Message = "Courses retrieved successfully",
                    Data = courses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses by faculty: {FacultyId}", facultyId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving courses",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get course by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetCourseById(string id)
        {
            try
            {
                var course = await _academicService.GetCourseByIdAsync(id);
                if (course == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Course not found"
                    });
                }

                return Ok(new ApiResponse<CourseDto>
                {
                    Success = true,
                    Message = "Course retrieved successfully",
                    Data = course
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course by ID: {CourseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the course",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Create a new course
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> CreateCourse(CreateCourseDto createCourseDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid course data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var course = await _academicService.CreateCourseAsync(createCourseDto);
                return Ok(new ApiResponse<CourseDto>
                {
                    Success = true,
                    Message = "Course created successfully",
                    Data = course
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
                _logger.LogError(ex, "Error creating course");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating the course",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update an existing course
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CourseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UpdateCourse(string id, CreateCourseDto updateCourseDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid course data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var course = await _academicService.UpdateCourseAsync(id, updateCourseDto);
                return Ok(new ApiResponse<CourseDto>
                {
                    Success = true,
                    Message = "Course updated successfully",
                    Data = course
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
                _logger.LogError(ex, "Error updating course: {CourseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating the course",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete a course (soft delete)
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> DeleteCourse(string id)
        {
            try
            {
                var result = await _academicService.DeleteCourseAsync(id);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Course deleted successfully"
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
                _logger.LogError(ex, "Error deleting course: {CourseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the course",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
