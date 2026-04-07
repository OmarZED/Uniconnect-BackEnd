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
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        /// <summary>
        /// Create a task for a subject (Teacher only; must own subject).
        /// </summary>
        [Authorize(Roles = "Teacher")]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> CreateTask(CreateTaskDto createTaskDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid task data",
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

                var task = await _taskService.CreateTaskAsync(createTaskDto, teacherId);
                return Ok(new ApiResponse<TaskDto>
                {
                    Success = true,
                    Message = "Task created successfully",
                    Data = task
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
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating the task",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get tasks for a subject (Teacher owner or Student member of subject community).
        /// </summary>
        [Authorize]
        [HttpGet("subject/{subjectId}")]
        [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), 200)]
        public async Task<IActionResult> GetTasksBySubject(string subjectId)
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

                var tasks = await _taskService.GetTasksBySubjectAsync(subjectId, userId);
                return Ok(new ApiResponse<List<TaskDto>>
                {
                    Success = true,
                    Message = "Tasks retrieved successfully",
                    Data = tasks
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
                _logger.LogError(ex, "Error getting tasks for subject: {SubjectId}", subjectId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving tasks",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get task by ID (Teacher owner or Student member).
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetTaskById(string id)
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

                var task = await _taskService.GetTaskByIdAsync(id, userId);
                if (task == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Task not found"
                    });
                }

                return Ok(new ApiResponse<TaskDto>
                {
                    Success = true,
                    Message = "Task retrieved successfully",
                    Data = task
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task by ID: {TaskId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the task",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update a task (Teacher only; must own subject).
        /// </summary>
        [Authorize(Roles = "Teacher")]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<TaskDto>), 200)]
        public async Task<IActionResult> UpdateTask(string id, UpdateTaskDto updateTaskDto)
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

                var task = await _taskService.UpdateTaskAsync(id, updateTaskDto, teacherId);
                return Ok(new ApiResponse<TaskDto>
                {
                    Success = true,
                    Message = "Task updated successfully",
                    Data = task
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
                _logger.LogError(ex, "Error updating task: {TaskId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating the task",
                    Errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete a task (Teacher only; must own subject).
        /// </summary>
        [Authorize(Roles = "Teacher")]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> DeleteTask(string id)
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

                await _taskService.DeleteTaskAsync(id, teacherId);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Task deleted successfully"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
                _logger.LogError(ex, "Error deleting task: {TaskId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the task",
                    Errors = new[] { ex.Message }
                });
            }
        }
    }
}
