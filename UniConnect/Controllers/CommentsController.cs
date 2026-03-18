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
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(ICommentService commentService, ILogger<CommentsController> logger)
        {
            _commentService = commentService;
            _logger = logger;
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CommentDto>), 200)]
        public async Task<IActionResult> CreateComment(CreateCommentDto createCommentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid comment data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                var comment = await _commentService.CreateCommentAsync(createCommentDto, userId);
                return Ok(new ApiResponse<CommentDto>
                {
                    Success = true,
                    Message = "Comment created successfully",
                    Data = comment
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating the comment",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("post/{postId}")]
        [ProducesResponseType(typeof(ApiResponse<List<CommentDto>>), 200)]
        public async Task<IActionResult> GetPostComments(string postId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var comments = await _commentService.GetPostCommentsAsync(postId, userId);
                return Ok(new ApiResponse<List<CommentDto>>
                {
                    Success = true,
                    Message = "Comments retrieved successfully",
                    Data = comments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for post: {PostId}", postId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving comments",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpPost("{commentId}/vote")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> VoteOnComment(string commentId, AddCommentVoteDto voteDto)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                await _commentService.VoteOnCommentAsync(commentId, userId, voteDto.VoteType);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Vote recorded"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voting on comment: {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while voting",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpPut("{commentId}")]
        [ProducesResponseType(typeof(ApiResponse<CommentDto>), 200)]
        public async Task<IActionResult> UpdateComment(string commentId, UpdateCommentDto updateCommentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid comment data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                var comment = await _commentService.UpdateCommentAsync(commentId, updateCommentDto, userId);
                return Ok(new ApiResponse<CommentDto>
                {
                    Success = true,
                    Message = "Comment updated successfully",
                    Data = comment
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
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment: {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating the comment",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpDelete("{commentId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> DeleteComment(string commentId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                await _commentService.DeleteCommentAsync(commentId, userId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Comment deleted successfully"
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
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment: {CommentId}", commentId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the comment",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
