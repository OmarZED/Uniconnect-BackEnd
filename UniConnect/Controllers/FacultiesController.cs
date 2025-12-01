using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniConnect.Dtos;
using UniConnect.INterfface;
using static UniConnect.Controllers.AuthController;

namespace UniConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class FacultiesController : ControllerBase
    {
        private readonly IAcademicService _academicService;
        private readonly ILogger<FacultiesController> _logger;

        public FacultiesController(IAcademicService academicService, ILogger<FacultiesController> logger)
        {
            _academicService = academicService;
            _logger = logger;
        }

        /// <summary>
        /// Get all active faculties
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<FacultyDto>>), 200)]
        public async Task<IActionResult> GetAllFaculties()
        {
            try
            {
                var faculties = await _academicService.GetAllFacultiesAsync();
                return Ok(new ApiResponse<List<FacultyDto>>
                {
                    Success = true,
                    Message = "Faculties retrieved successfully",
                    Data = faculties
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all faculties");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving faculties",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get faculty by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FacultyDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetFacultyById(string id)
        {
            try
            {
                var faculty = await _academicService.GetFacultyByIdAsync(id);
                if (faculty == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Faculty not found"
                    });
                }

                return Ok(new ApiResponse<FacultyDto>
                {
                    Success = true,
                    Message = "Faculty retrieved successfully",
                    Data = faculty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting faculty by ID: {FacultyId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the faculty",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Create a new faculty
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FacultyDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> CreateFaculty(CreateFacultyDto createFacultyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid faculty data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Automatically set the deanId from the logged-in user if they are a Dean
                var userRole = User.FindFirst("Role")?.Value;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userRole == "Dean" && string.IsNullOrEmpty(createFacultyDto.DeanId))
                {
                    createFacultyDto.DeanId = userId;
                }

                var faculty = await _academicService.CreateFacultyAsync(createFacultyDto);
                return Ok(new ApiResponse<FacultyDto>
                {
                    Success = true,
                    Message = "Faculty created successfully",
                    Data = faculty
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
                _logger.LogError(ex, "Error creating faculty");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating the faculty",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update an existing faculty
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FacultyDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UpdateFaculty(string id, CreateFacultyDto updateFacultyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid faculty data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var faculty = await _academicService.UpdateFacultyAsync(id, updateFacultyDto);
                return Ok(new ApiResponse<FacultyDto>
                {
                    Success = true,
                    Message = "Faculty updated successfully",
                    Data = faculty
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
                _logger.LogError(ex, "Error updating faculty: {FacultyId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating the faculty",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete a faculty (soft delete)
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> DeleteFaculty(string id)
        {
            try
            {
                var result = await _academicService.DeleteFacultyAsync(id);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Faculty deleted successfully"
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
                _logger.LogError(ex, "Error deleting faculty: {FacultyId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the faculty",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get active faculties for dropdowns
        /// </summary>
        [HttpGet("active")]
        [ProducesResponseType(typeof(ApiResponse<List<FacultyDto>>), 200)]
        public async Task<IActionResult> GetActiveFaculties()
        {
            try
            {
                var faculties = await _academicService.GetActiveFacultiesAsync();
                return Ok(new ApiResponse<List<FacultyDto>>
                {
                    Success = true,
                    Message = "Active faculties retrieved successfully",
                    Data = faculties
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active faculties");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving active faculties",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
