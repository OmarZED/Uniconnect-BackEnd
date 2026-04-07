using Microsoft.EntityFrameworkCore;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Maping;
using UniConnect.Models;

namespace UniConnect.Repository
{
    public class SubmissionService : ISubmissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICommunityService _communityService;
        private readonly ILogger<SubmissionService> _logger;

        public SubmissionService(ApplicationDbContext context, ICommunityService communityService, ILogger<SubmissionService> logger)
        {
            _context = context;
            _communityService = communityService;
            _logger = logger;
        }

        public async Task<SubmissionDto> CreateSubmissionAsync(CreateSubmissionDto createSubmissionDto, string studentId)
        {
            try
            {
                var task = await _context.TaskAssignments
                    .Include(t => t.Subject)
                    .ThenInclude(s => s.Teacher)
                    .FirstOrDefaultAsync(t => t.Id == createSubmissionDto.TaskId && t.IsActive);

                if (task == null)
                {
                    throw new InvalidOperationException("Task not found.");
                }

                await EnsureStudentIsSubjectMember(task.SubjectId, studentId);

                var existing = await _context.Submissions
                    .Include(s => s.Student)
                    .Include(s => s.Task)
                    .ThenInclude(t => t.Subject)
                    .FirstOrDefaultAsync(s => s.TaskId == createSubmissionDto.TaskId && s.StudentId == studentId && s.IsActive);

                if (existing != null)
                {
                    if (existing.Status == SubmissionStatus.Graded)
                    {
                        throw new InvalidOperationException("Submission already graded and cannot be updated.");
                    }

                    existing.Content = createSubmissionDto.Content;
                    existing.AttachmentUrl = createSubmissionDto.AttachmentUrl;
                    existing.Status = SubmissionStatus.Submitted;
                    existing.SubmittedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    return MapToSubmissionDto(existing);
                }

                var submission = new Submission
                {
                    TaskId = createSubmissionDto.TaskId,
                    StudentId = studentId,
                    Content = createSubmissionDto.Content,
                    AttachmentUrl = createSubmissionDto.AttachmentUrl,
                    Status = SubmissionStatus.Submitted,
                    SubmittedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();

                await _context.Entry(submission).Reference(s => s.Student).LoadAsync();
                await _context.Entry(submission).Reference(s => s.Task).LoadAsync();
                await _context.Entry(submission.Task).Reference(t => t.Subject).LoadAsync();

                return MapToSubmissionDto(submission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating submission for task: {TaskId}", createSubmissionDto.TaskId);
                throw;
            }
        }

        public async Task<List<SubmissionDto>> GetSubmissionsByTaskAsync(string taskId, string teacherId)
        {
            try
            {
                var task = await _context.TaskAssignments
                    .Include(t => t.Subject)
                    .FirstOrDefaultAsync(t => t.Id == taskId && t.IsActive);

                if (task == null)
                {
                    throw new InvalidOperationException("Task not found.");
                }

                if (!string.Equals(task.Subject?.TeacherId, teacherId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Only the subject owner can view submissions.");
                }

                var submissions = await _context.Submissions
                    .Include(s => s.Student)
                    .Include(s => s.Task)
                    .Where(s => s.TaskId == taskId && s.IsActive)
                    .OrderByDescending(s => s.SubmittedAt)
                    .ToListAsync();

                return submissions.Select(MapToSubmissionDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submissions for task: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<SubmissionDto?> GetSubmissionByIdAsync(string id, string userId, bool isTeacher)
        {
            try
            {
                var submission = await _context.Submissions
                    .Include(s => s.Student)
                    .Include(s => s.Task)
                        .ThenInclude(t => t.Subject)
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

                if (submission == null) return null;

                if (isTeacher)
                {
                    if (!string.Equals(submission.Task.Subject?.TeacherId, userId, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new UnauthorizedAccessException("Teacher not authorized to view this submission.");
                    }
                }
                else
                {
                    if (!string.Equals(submission.StudentId, userId, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new UnauthorizedAccessException("Student not authorized to view this submission.");
                    }
                }

                return MapToSubmissionDto(submission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission by ID: {SubmissionId}", id);
                throw;
            }
        }

        public async Task<SubmissionDto> UpdateSubmissionAsync(string id, UpdateSubmissionDto updateSubmissionDto, string studentId)
        {
            try
            {
                var submission = await _context.Submissions
                    .Include(s => s.Student)
                    .Include(s => s.Task)
                        .ThenInclude(t => t.Subject)
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

                if (submission == null)
                {
                    throw new KeyNotFoundException("Submission not found.");
                }

                if (!string.Equals(submission.StudentId, studentId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Student not authorized to update this submission.");
                }

                if (submission.Status == SubmissionStatus.Graded)
                {
                    throw new InvalidOperationException("Submission already graded and cannot be updated.");
                }

                submission.Content = updateSubmissionDto.Content;
                submission.AttachmentUrl = updateSubmissionDto.AttachmentUrl;
                submission.Status = SubmissionStatus.Submitted;
                submission.SubmittedAt = DateTime.UtcNow;
                submission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return MapToSubmissionDto(submission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating submission: {SubmissionId}", id);
                throw;
            }
        }

        public async Task<SubmissionDto> GradeSubmissionAsync(string id, GradeSubmissionDto gradeDto, string teacherId)
        {
            try
            {
                var submission = await _context.Submissions
                    .Include(s => s.Student)
                    .Include(s => s.Task)
                        .ThenInclude(t => t.Subject)
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

                if (submission == null)
                {
                    throw new KeyNotFoundException("Submission not found.");
                }

                if (!string.Equals(submission.Task.Subject?.TeacherId, teacherId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Teacher not authorized to grade this submission.");
                }

                submission.Grade = gradeDto.Grade;
                submission.Feedback = gradeDto.Feedback;
                submission.Status = SubmissionStatus.Graded;
                submission.GradedAt = DateTime.UtcNow;
                submission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return MapToSubmissionDto(submission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error grading submission: {SubmissionId}", id);
                throw;
            }
        }

        private async Task EnsureStudentIsSubjectMember(string subjectId, string studentId)
        {
            var community = await _context.Communities
                .FirstOrDefaultAsync(c => c.SubjectId == subjectId && c.Type == CommunityType.Subject && c.IsActive);

            if (community == null)
            {
                await _communityService.GetOrCreateSubjectCommunityAsync(subjectId);
                community = await _context.Communities
                    .FirstOrDefaultAsync(c => c.SubjectId == subjectId && c.Type == CommunityType.Subject && c.IsActive);
            }

            if (community == null)
            {
                throw new InvalidOperationException("Subject community not found.");
            }

            var isMember = await _context.CommunityMembers
                .AnyAsync(cm => cm.CommunityId == community.Id && cm.UserId == studentId && cm.IsActive);

            if (!isMember)
            {
                throw new UnauthorizedAccessException("Student is not a member of this subject.");
            }
        }

        private static SubmissionDto MapToSubmissionDto(Submission submission)
        {
            return new SubmissionDto
            {
                Id = submission.Id,
                TaskId = submission.TaskId,
                TaskTitle = submission.Task?.Title ?? string.Empty,
                StudentId = submission.StudentId,
                StudentName = submission.Student != null
                    ? $"{submission.Student.FirstName} {submission.Student.LastName}"
                    : string.Empty,
                Content = submission.Content,
                AttachmentUrl = submission.AttachmentUrl,
                Status = submission.Status,
                SubmittedAt = submission.SubmittedAt,
                Grade = submission.Grade,
                Feedback = submission.Feedback,
                GradedAt = submission.GradedAt,
                CreatedAt = submission.CreatedAt,
                UpdatedAt = submission.UpdatedAt,
                IsActive = submission.IsActive
            };
        }
    }
}
