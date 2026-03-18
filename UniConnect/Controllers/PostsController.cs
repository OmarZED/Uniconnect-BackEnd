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
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ILogger<PostsController> _logger;

        public PostsController(IPostService postService, ILogger<PostsController> logger)
        {
            _postService = postService;
            _logger = logger;
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
        public async Task<IActionResult> CreatePost(CreatePostDto createPostDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid post data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                var post = await _postService.CreatePostAsync(createPostDto, userId);
                return Ok(new ApiResponse<PostDto>
                {
                    Success = true,
                    Message = "Post created successfully",
                    Data = post
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating the post",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("community/{communityId}")]
        [ProducesResponseType(typeof(ApiResponse<List<PostDto>>), 200)]
        public async Task<IActionResult> GetCommunityPosts(string communityId, int page = 1, int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                var posts = await _postService.GetCommunityPostsAsync(communityId, userId, page, pageSize);
                return Ok(new ApiResponse<List<PostDto>>
                {
                    Success = true,
                    Message = "Posts retrieved successfully",
                    Data = posts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community posts");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving posts",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
        public async Task<IActionResult> GetPostById(string id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                var post = await _postService.GetPostByIdAsync(id, userId);
                if (post == null)
                {
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Post not found" });
                }

                return Ok(new ApiResponse<PostDto>
                {
                    Success = true,
                    Message = "Post retrieved successfully",
                    Data = post
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post by id: {PostId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the post",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PostDto>), 200)]
        public async Task<IActionResult> UpdatePost(string id, UpdatePostDto updatePostDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid post data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                var post = await _postService.UpdatePostAsync(id, updatePostDto, userId);
                return Ok(new ApiResponse<PostDto>
                {
                    Success = true,
                    Message = "Post updated successfully",
                    Data = post
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
                _logger.LogError(ex, "Error updating post: {PostId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating the post",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> DeletePost(string id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                await _postService.DeletePostAsync(id, userId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Post deleted successfully"
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
                _logger.LogError(ex, "Error deleting post: {PostId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the post",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpGet("feed")]
        [ProducesResponseType(typeof(ApiResponse<List<PostDto>>), 200)]
        public async Task<IActionResult> GetUserFeed(int page = 1, int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                var posts = await _postService.GetUserFeedAsync(userId, page, pageSize);
                return Ok(new ApiResponse<List<PostDto>>
                {
                    Success = true,
                    Message = "Feed retrieved successfully",
                    Data = posts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user feed");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the feed",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpPost("{id}/react")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> ReactToPost(string id, AddPostReactionDto reactionDto)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                await _postService.AddPostReactionAsync(id, userId, reactionDto.ReactionType);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Reaction added"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to post: {PostId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while reacting to the post",
                    Errors = new[] { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpDelete("{id}/react")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> RemoveReaction(string id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object> { Success = false, Message = "User not authenticated" });
                }

                await _postService.RemovePostReactionAsync(id, userId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Reaction removed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing reaction: {PostId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while removing reaction",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
