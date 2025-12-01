using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Models;
using static UniConnect.Controllers.AuthController;

namespace UniConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CommunitiesController : ControllerBase
    {
        private readonly ICommunityService _communityService;
        private readonly ILogger<CommunitiesController> _logger;

        public CommunitiesController(ICommunityService communityService, ILogger<CommunitiesController> logger)
        {
            _communityService = communityService;
            _logger = logger;
        }

        /// <summary>
        /// Get all active communities
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CommunityDto>>), 200)]
        public async Task<IActionResult> GetAllCommunities()
        {
            try
            {
                var communities = await _communityService.GetAllCommunitiesAsync();
                return Ok(new ApiResponse<List<CommunityDto>>
                {
                    Success = true,
                    Message = "Communities retrieved successfully",
                    Data = communities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all communities");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving communities",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get community by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CommunityDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetCommunityById(string id)
        {
            try
            {
                var community = await _communityService.GetCommunityByIdAsync(id);
                if (community == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Community not found"
                    });
                }

                return Ok(new ApiResponse<CommunityDto>
                {
                    Success = true,
                    Message = "Community retrieved successfully",
                    Data = community
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community by ID: {CommunityId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the community",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPost("department")]
        [ProducesResponseType(typeof(ApiResponse<CommunityDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> CreateDepartmentCommunity(CreateDepartmentCommunityDto createDepartmentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid community data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var community = await _communityService.CreateDepartmentCommunityAsync(createDepartmentDto);
                return Ok(new ApiResponse<CommunityDto>
                {
                    Success = true,
                    Message = "Department community created successfully",
                    Data = community
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department community");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating the department community",
                    Errors = new[] { ex.Message }
                });
            }
        }
        /// <summary>
        /// Update an existing community
        /// </summary>
        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CommunityDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UpdateCommunity(string id, UpdateCommunityDto updateCommunityDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid community data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var community = await _communityService.UpdateCommunityAsync(id, updateCommunityDto);
                return Ok(new ApiResponse<CommunityDto>
                {
                    Success = true,
                    Message = "Community updated successfully",
                    Data = community
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
                _logger.LogError(ex, "Error updating community: {CommunityId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating the community",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete a community (soft delete) - Only department communities can be deleted manually
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> DeleteCommunity(string id)
        {
            try
            {
                var result = await _communityService.DeleteCommunityAsync(id);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Community deleted successfully"
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
                _logger.LogError(ex, "Error deleting community: {CommunityId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the community",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Join a community
        /// </summary>
        [Authorize]
        [HttpPost("{id}/join")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> JoinCommunity(string id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var result = await _communityService.JoinCommunityAsync(id, userId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Joined community successfully"
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
                _logger.LogError(ex, "Error joining community: {CommunityId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while joining the community",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Leave a community
        /// </summary>
        [Authorize]
        [HttpPost("{id}/leave")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> LeaveCommunity(string id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var result = await _communityService.LeaveCommunityAsync(id, userId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Left community successfully"
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
                _logger.LogError(ex, "Error leaving community: {CommunityId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while leaving the community",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get community members
        /// </summary>
        [HttpGet("{id}/members")]
        [ProducesResponseType(typeof(ApiResponse<List<CommunityMemberDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetCommunityMembers(string id)
        {
            try
            {
                var members = await _communityService.GetCommunityMembersAsync(id);
                return Ok(new ApiResponse<List<CommunityMemberDto>>
                {
                    Success = true,
                    Message = "Community members retrieved successfully",
                    Data = members
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community members for community: {CommunityId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving community members",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get or create faculty community (for internal use - academic communities are now created automatically)
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPost("faculty/{facultyId}")]
        [ProducesResponseType(typeof(ApiResponse<CommunityDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetOrCreateFacultyCommunity(string facultyId)
        {
            try
            {
                var community = await _communityService.GetOrCreateFacultyCommunityAsync(facultyId);
                return Ok(new ApiResponse<CommunityDto>
                {
                    Success = true,
                    Message = "Faculty community retrieved/created successfully",
                    Data = community
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
                _logger.LogError(ex, "Error getting/creating faculty community: {FacultyId}", facultyId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while getting/creating faculty community",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get or create course community (for internal use - academic communities are now created automatically)
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPost("course/{courseId}")]
        [ProducesResponseType(typeof(ApiResponse<CommunityDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetOrCreateCourseCommunity(string courseId)
        {
            try
            {
                var community = await _communityService.GetOrCreateCourseCommunityAsync(courseId);
                return Ok(new ApiResponse<CommunityDto>
                {
                    Success = true,
                    Message = "Course community retrieved/created successfully",
                    Data = community
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
                _logger.LogError(ex, "Error getting/creating course community: {CourseId}", courseId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while getting/creating course community",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get or create group community (for internal use - academic communities are now created automatically)
        /// </summary>
        [Authorize(Roles = "Dean,DepartmentManager")]
        [HttpPost("group/{groupId}")]
        [ProducesResponseType(typeof(ApiResponse<CommunityDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetOrCreateGroupCommunity(string groupId)
        {
            try
            {
                var community = await _communityService.GetOrCreateGroupCommunityAsync(groupId);
                return Ok(new ApiResponse<CommunityDto>
                {
                    Success = true,
                    Message = "Group community retrieved/created successfully",
                    Data = community
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
                _logger.LogError(ex, "Error getting/creating group community: {GroupId}", groupId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while getting/creating group community",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get current user's communities
        /// </summary>
        [Authorize]
        [HttpGet("my-communities")]
        [ProducesResponseType(typeof(ApiResponse<List<CommunityDto>>), 200)]
        public async Task<IActionResult> GetMyCommunities()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var communities = await _communityService.GetUserCommunitiesAsync(userId);
                return Ok(new ApiResponse<List<CommunityDto>>
                {
                    Success = true,
                    Message = "User communities retrieved successfully",
                    Data = communities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user communities");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving user communities",
                    Errors = new[] { ex.Message }
                });
            }
        }


    }
}