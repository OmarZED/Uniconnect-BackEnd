using Microsoft.EntityFrameworkCore;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Maping;
using UniConnect.Models;

namespace UniConnect.Repository
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ApplicationDbContext context, ILogger<TaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TaskDto> CreateTaskAsync(CreateTaskDto createTaskDto, string teacherId)
        {
            try
            {
                var subject = await _context.Subjects
                    .Include(s => s.Teacher)
                    .FirstOrDefaultAsync(s => s.Id == createTaskDto.SubjectId && s.IsActive);

                if (subject == null)
                {
                    throw new InvalidOperationException("Subject not found.");
                }

                if (!string.Equals(subject.TeacherId, teacherId, StringComparison.OrdinalIgnoreCase))
                {
                    // Only the subject owner can create tasks.
                    throw new UnauthorizedAccessException("Only the subject owner can create tasks.");
                }

                var task = new TaskAssignment
                {
                    Title = createTaskDto.Title,
                    Description = createTaskDto.Description,
                    DueDate = createTaskDto.DueDate,
                    MaxScore = createTaskDto.MaxScore,
                    SubjectId = createTaskDto.SubjectId,
                    CreatedByTeacherId = teacherId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.TaskAssignments.Add(task);
                await _context.SaveChangesAsync();

                return MapToTaskDto(task, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task for subject: {SubjectId}", createTaskDto.SubjectId);
                throw;
            }
        }

        public async Task<List<TaskDto>> GetTasksBySubjectAsync(string subjectId, string userId)
        {
            try
            {
                var subject = await _context.Subjects
                    .Include(s => s.Teacher)
                    .FirstOrDefaultAsync(s => s.Id == subjectId && s.IsActive);

                if (subject == null)
                {
                    throw new InvalidOperationException("Subject not found.");
                }

                var isTeacherOwner = string.Equals(subject.TeacherId, userId, StringComparison.OrdinalIgnoreCase);
                var isMember = await _context.CommunityMembers
                    .Include(cm => cm.Community)
                    .AnyAsync(cm =>
                        cm.UserId == userId &&
                        cm.IsActive &&
                        cm.Community.IsActive &&
                        cm.Community.Type == CommunityType.Subject &&
                        cm.Community.SubjectId == subjectId);

                if (!isTeacherOwner && !isMember)
                {
                    // Teachers own subjects; students must be subject community members to view tasks.
                    throw new UnauthorizedAccessException("User is not authorized to view tasks for this subject.");
                }

                var tasks = await _context.TaskAssignments
                    .Where(t => t.SubjectId == subjectId && t.IsActive)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return tasks.Select(t => MapToTaskDto(t, subject)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for subject: {SubjectId}", subjectId);
                throw;
            }
        }

        public async Task<TaskDto?> GetTaskByIdAsync(string id, string userId)
        {
            try
            {
                var task = await _context.TaskAssignments
                    .Include(t => t.Subject)
                        .ThenInclude(s => s.Teacher)
                    .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

                if (task == null) return null;

                var isTeacherOwner = string.Equals(task.Subject?.TeacherId, userId, StringComparison.OrdinalIgnoreCase);
                var isMember = await _context.CommunityMembers
                    .Include(cm => cm.Community)
                    .AnyAsync(cm =>
                        cm.UserId == userId &&
                        cm.IsActive &&
                        cm.Community.IsActive &&
                        cm.Community.Type == CommunityType.Subject &&
                        cm.Community.SubjectId == task.SubjectId);

                if (!isTeacherOwner && !isMember)
                {
                    throw new UnauthorizedAccessException("User is not authorized to view this task.");
                }

                return MapToTaskDto(task, task.Subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task by ID: {TaskId}", id);
                throw;
            }
        }

        public async Task<TaskDto> UpdateTaskAsync(string id, UpdateTaskDto updateTaskDto, string teacherId)
        {
            try
            {
                var task = await _context.TaskAssignments
                    .Include(t => t.Subject)
                        .ThenInclude(s => s.Teacher)
                    .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

                if (task == null)
                {
                    throw new KeyNotFoundException("Task not found.");
                }

                if (!string.Equals(task.Subject?.TeacherId, teacherId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Only the subject owner can update tasks.");
                }

                if (!string.IsNullOrWhiteSpace(updateTaskDto.Title))
                {
                    task.Title = updateTaskDto.Title;
                }

                if (!string.IsNullOrWhiteSpace(updateTaskDto.Description))
                {
                    task.Description = updateTaskDto.Description;
                }

                if (updateTaskDto.DueDate.HasValue)
                {
                    task.DueDate = updateTaskDto.DueDate.Value;
                }

                if (updateTaskDto.MaxScore.HasValue)
                {
                    task.MaxScore = updateTaskDto.MaxScore.Value;
                }

                task.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return MapToTaskDto(task, task.Subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task: {TaskId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteTaskAsync(string id, string teacherId)
        {
            try
            {
                var task = await _context.TaskAssignments
                    .Include(t => t.Subject)
                    .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

                if (task == null)
                {
                    throw new KeyNotFoundException("Task not found.");
                }

                if (!string.Equals(task.Subject?.TeacherId, teacherId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Only the subject owner can delete tasks.");
                }

                task.IsActive = false;
                task.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task: {TaskId}", id);
                throw;
            }
        }

        private static TaskDto MapToTaskDto(TaskAssignment task, Subject? subject)
        {
            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                MaxScore = task.MaxScore,
                SubjectId = task.SubjectId,
                SubjectName = subject?.Name ?? string.Empty,
                CreatedByTeacherId = task.CreatedByTeacherId,
                CreatedByTeacherName = subject?.Teacher != null
                    ? $"{subject.Teacher.FirstName} {subject.Teacher.LastName}"
                    : string.Empty,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                IsActive = task.IsActive
            };
        }
    }
}
