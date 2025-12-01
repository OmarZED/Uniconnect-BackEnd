using Microsoft.EntityFrameworkCore;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Maping;
using UniConnect.Models;

namespace UniConnect.Repository
{
    public class AcademicService : IAcademicService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AcademicService> _logger;
        private readonly ICommunityService _communityService;
        public AcademicService(ApplicationDbContext context, ICommunityService communityService, ILogger<AcademicService> logger)
        {
            _context = context;
            _logger = logger;
            _communityService = communityService;
        }

        // ========== FACULTY METHODS ==========

        public async Task<List<FacultyDto>> GetAllFacultiesAsync()
        {
            try
            {
                var faculties = await _context.Faculties
                    .Include(f => f.Dean)
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                return faculties.Select(f => new FacultyDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Code = f.Code,
                    Description = f.Description,
                    DeanId = f.DeanId,
                    DeanName = f.Dean != null ? $"{f.Dean.FirstName} {f.Dean.LastName}" : null,
                    CreatedAt = f.CreatedAt,
                    IsActive = f.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all faculties");
                throw;
            }
        }

        public async Task<FacultyDto> GetFacultyByIdAsync(string id)
        {
            try
            {
                var faculty = await _context.Faculties
                    .Include(f => f.Dean)
                    .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

                if (faculty == null) return null;

                return new FacultyDto
                {
                    Id = faculty.Id,
                    Name = faculty.Name,
                    Code = faculty.Code,
                    Description = faculty.Description,
                    DeanId = faculty.DeanId,
                    DeanName = faculty.Dean != null ? $"{faculty.Dean.FirstName} {faculty.Dean.LastName}" : null,
                    CreatedAt = faculty.CreatedAt,
                    IsActive = faculty.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting faculty by ID: {FacultyId}", id);
                throw;
            }
        }

        public async Task<FacultyDto> CreateFacultyAsync(CreateFacultyDto createFacultyDto)
        {
            try
            {
                // Check if faculty code already exists
                var existingFaculty = await _context.Faculties
                    .FirstOrDefaultAsync(f => f.Code == createFacultyDto.Code && f.IsActive);

                if (existingFaculty != null)
                {
                    throw new InvalidOperationException($"Faculty with code '{createFacultyDto.Code}' already exists.");
                }

                var faculty = new Faculty
                {
                    Name = createFacultyDto.Name,
                    Code = createFacultyDto.Code,
                    Description = createFacultyDto.Description,
                    DeanId = createFacultyDto.DeanId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Faculties.Add(faculty);
                await _context.SaveChangesAsync();

                // Use CommunityService to create the community
                try
                {
                    await _communityService.GetOrCreateFacultyCommunityAsync(faculty.Id);
                    _logger.LogInformation("Automatically created community for faculty: {FacultyName}", faculty.Name);
                }
                catch (Exception commEx)
                {
                    _logger.LogError(commEx, "Failed to automatically create community for faculty: {FacultyName}", faculty.Name);
                    // Don't throw here - we don't want faculty creation to fail if community creation fails
                }

                // Reload with dean information
                await _context.Entry(faculty)
                    .Reference(f => f.Dean)
                    .LoadAsync();

                return new FacultyDto
                {
                    Id = faculty.Id,
                    Name = faculty.Name,
                    Code = faculty.Code,
                    Description = faculty.Description,
                    DeanId = faculty.DeanId,
                    DeanName = faculty.Dean != null ? $"{faculty.Dean.FirstName} {faculty.Dean.LastName}" : null,
                    CreatedAt = faculty.CreatedAt,
                    IsActive = faculty.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating faculty: {FacultyName}", createFacultyDto.Name);
                throw;
            }
        }
        

        public async Task<FacultyDto> UpdateFacultyAsync(string id, CreateFacultyDto updateFacultyDto)
        {
            try
            {
                var faculty = await _context.Faculties
                    .Include(f => f.Dean)
                    .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

                if (faculty == null)
                {
                    throw new KeyNotFoundException($"Faculty with ID '{id}' not found.");
                }

                // Check if code is being changed and if it conflicts with another faculty
                if (faculty.Code != updateFacultyDto.Code)
                {
                    var existingFaculty = await _context.Faculties
                        .FirstOrDefaultAsync(f => f.Code == updateFacultyDto.Code && f.Id != id && f.IsActive);

                    if (existingFaculty != null)
                    {
                        throw new InvalidOperationException($"Faculty with code '{updateFacultyDto.Code}' already exists.");
                    }
                }

                faculty.Name = updateFacultyDto.Name;
                faculty.Code = updateFacultyDto.Code;
                faculty.Description = updateFacultyDto.Description;
                faculty.DeanId = updateFacultyDto.DeanId;

                await _context.SaveChangesAsync();

                return new FacultyDto
                {
                    Id = faculty.Id,
                    Name = faculty.Name,
                    Code = faculty.Code,
                    Description = faculty.Description,
                    DeanId = faculty.DeanId,
                    DeanName = faculty.Dean != null ? $"{faculty.Dean.FirstName} {faculty.Dean.LastName}" : null,
                    CreatedAt = faculty.CreatedAt,
                    IsActive = faculty.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating faculty: {FacultyId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteFacultyAsync(string id)
        {
            try
            {
                var faculty = await _context.Faculties
                    .Include(f => f.Courses)
                    .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

                if (faculty == null)
                {
                    throw new KeyNotFoundException($"Faculty with ID '{id}' not found.");
                }

                // Check if faculty has courses
                if (faculty.Courses.Any(c => c.IsActive))
                {
                    throw new InvalidOperationException("Cannot delete faculty that has active courses. Please delete or reassign the courses first.");
                }

                // Soft delete
                faculty.IsActive = false;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting faculty: {FacultyId}", id);
                throw;
            }
        }

        public async Task<List<FacultyDto>> GetActiveFacultiesAsync()
        {
            try
            {
                var faculties = await _context.Faculties
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                return faculties.Select(f => new FacultyDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Code = f.Code,
                    Description = f.Description,
                    CreatedAt = f.CreatedAt,
                    IsActive = f.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active faculties");
                throw;
            }
        }

        // ========== COURSE METHODS (Stubs for now) ==========
        public async Task<List<CourseDto>> GetAllCoursesAsync()
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.Faculty)
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Faculty.Name)
                    .ThenBy(c => c.Year)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                return courses.Select(c => new CourseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Year = c.Year,
                    Code = c.Code,
                    FacultyId = c.FacultyId,
                    FacultyName = c.Faculty.Name,
                    CreatedAt = c.CreatedAt,
                    IsActive = c.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all courses");
                throw;
            }
        }
        public async Task<List<CourseDto>> GetCoursesByFacultyAsync(string facultyId)
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.Faculty)
                    .Where(c => c.FacultyId == facultyId && c.IsActive)
                    .OrderBy(c => c.Year)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                return courses.Select(c => new CourseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Year = c.Year,
                    Code = c.Code,
                    FacultyId = c.FacultyId,
                    FacultyName = c.Faculty.Name,
                    CreatedAt = c.CreatedAt,
                    IsActive = c.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses by faculty: {FacultyId}", facultyId);
                throw;
            }
        }

        public async Task<CourseDto> GetCourseByIdAsync(string id)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Faculty)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (course == null) return null;

                return new CourseDto
                {
                    Id = course.Id,
                    Name = course.Name,
                    Year = course.Year,
                    Code = course.Code,
                    FacultyId = course.FacultyId,
                    FacultyName = course.Faculty.Name,
                    CreatedAt = course.CreatedAt,
                    IsActive = course.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course by ID: {CourseId}", id);
                throw;
            }
        }
        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto createCourseDto)
        {
            try
            {
                // Verify faculty exists
                var faculty = await _context.Faculties
                    .FirstOrDefaultAsync(f => f.Id == createCourseDto.FacultyId && f.IsActive);

                if (faculty == null)
                {
                    throw new InvalidOperationException($"Faculty with ID '{createCourseDto.FacultyId}' not found.");
                }

                // Check if course code already exists in the same faculty
                var existingCourse = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Code == createCourseDto.Code &&
                                           c.FacultyId == createCourseDto.FacultyId &&
                                           c.IsActive);

                if (existingCourse != null)
                {
                    throw new InvalidOperationException($"Course with code '{createCourseDto.Code}' already exists in this faculty.");
                }

                var course = new Course
                {
                    Name = createCourseDto.Name,
                    Year = createCourseDto.Year,
                    Code = createCourseDto.Code,
                    FacultyId = createCourseDto.FacultyId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                // Use CommunityService to create the community
                try
                {
                    await _communityService.GetOrCreateCourseCommunityAsync(course.Id);
                    _logger.LogInformation("Automatically created community for course: {CourseName}", course.Name);
                }
                catch (Exception commEx)
                {
                    _logger.LogError(commEx, "Failed to automatically create community for course: {CourseName}", course.Name);
                    // Don't throw here - we don't want course creation to fail if community creation fails
                }

                // Reload with faculty information
                await _context.Entry(course)
                    .Reference(c => c.Faculty)
                    .LoadAsync();

                return new CourseDto
                {
                    Id = course.Id,
                    Name = course.Name,
                    Year = course.Year,
                    Code = course.Code,
                    FacultyId = course.FacultyId,
                    FacultyName = course.Faculty.Name,
                    CreatedAt = course.CreatedAt,
                    IsActive = course.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course: {CourseName}", createCourseDto.Name);
                throw;
            }
        }
        public async Task<CourseDto> UpdateCourseAsync(string id, CreateCourseDto updateCourseDto)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Faculty)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (course == null)
                {
                    throw new KeyNotFoundException($"Course with ID '{id}' not found.");
                }

                // Verify faculty exists if changing faculty
                if (course.FacultyId != updateCourseDto.FacultyId)
                {
                    var faculty = await _context.Faculties
                        .FirstOrDefaultAsync(f => f.Id == updateCourseDto.FacultyId && f.IsActive);

                    if (faculty == null)
                    {
                        throw new InvalidOperationException($"Faculty with ID '{updateCourseDto.FacultyId}' not found.");
                    }
                }

                // Check if code is being changed and if it conflicts
                if (course.Code != updateCourseDto.Code || course.FacultyId != updateCourseDto.FacultyId)
                {
                    var existingCourse = await _context.Courses
                        .FirstOrDefaultAsync(c => c.Code == updateCourseDto.Code &&
                                               c.FacultyId == updateCourseDto.FacultyId &&
                                               c.Id != id &&
                                               c.IsActive);

                    if (existingCourse != null)
                    {
                        throw new InvalidOperationException($"Course with code '{updateCourseDto.Code}' already exists in this faculty.");
                    }
                }

                course.Name = updateCourseDto.Name;
                course.Year = updateCourseDto.Year;
                course.Code = updateCourseDto.Code;
                course.FacultyId = updateCourseDto.FacultyId;

                await _context.SaveChangesAsync();

                return new CourseDto
                {
                    Id = course.Id,
                    Name = course.Name,
                    Year = course.Year,
                    Code = course.Code,
                    FacultyId = course.FacultyId,
                    FacultyName = course.Faculty.Name,
                    CreatedAt = course.CreatedAt,
                    IsActive = course.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course: {CourseId}", id);
                throw;
            }
        }
        public async Task<bool> DeleteCourseAsync(string id)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Groups)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (course == null)
                {
                    throw new KeyNotFoundException($"Course with ID '{id}' not found.");
                }

                // Check if course has groups
                if (course.Groups.Any(g => g.IsActive))
                {
                    throw new InvalidOperationException("Cannot delete course that has active groups. Please delete or reassign the groups first.");
                }

                // Soft delete
                course.IsActive = false;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course: {CourseId}", id);
                throw;
            }
        }

        // ========== STUDENT GROUP METHODS (Stubs for now) ==========
        public async Task<List<StudentGroupDto>> GetAllGroupsAsync()
        {
            try
            {
                var groups = await _context.StudentGroups
                    .Include(g => g.Course)
                    .ThenInclude(c => c.Faculty)
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.Course.Faculty.Name)
                    .ThenBy(g => g.Course.Year)
                    .ThenBy(g => g.Course.Name)
                    .ThenBy(g => g.Name)
                    .ToListAsync();

                return groups.Select(g => new StudentGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Code = g.Code,
                    Description = g.Description,
                    CourseId = g.CourseId,
                    CourseName = g.Course.Name,
                    FacultyId = g.Course.FacultyId,
                    FacultyName = g.Course.Faculty.Name,
                    CreatedAt = g.CreatedAt,
                    IsActive = g.IsActive,
                    StudentCount = g.Students.Count(s => s.IsActive)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all groups");
                throw;
            }
        }

        public async Task<List<StudentGroupDto>> GetGroupsByCourseAsync(string courseId)
        {
            try
            {
                var groups = await _context.StudentGroups
                    .Include(g => g.Course)
                    .ThenInclude(c => c.Faculty)
                    .Where(g => g.CourseId == courseId && g.IsActive)
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                return groups.Select(g => new StudentGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Code = g.Code,
                    Description = g.Description,
                    CourseId = g.CourseId,
                    CourseName = g.Course.Name,
                    FacultyId = g.Course.FacultyId,
                    FacultyName = g.Course.Faculty.Name,
                    CreatedAt = g.CreatedAt,
                    IsActive = g.IsActive,
                    StudentCount = g.Students.Count(s => s.IsActive)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups by course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<StudentGroupDto> GetGroupByIdAsync(string id)
        {
            try
            {
                var group = await _context.StudentGroups
                    .Include(g => g.Course)
                    .ThenInclude(c => c.Faculty)
                    .Include(g => g.Students)
                    .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);

                if (group == null) return null;

                return new StudentGroupDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    Code = group.Code,
                    Description = group.Description,
                    CourseId = group.CourseId,
                    CourseName = group.Course.Name,
                    FacultyId = group.Course.FacultyId,
                    FacultyName = group.Course.Faculty.Name,
                    CreatedAt = group.CreatedAt,
                    IsActive = group.IsActive,
                    StudentCount = group.Students.Count(s => s.IsActive)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group by ID: {GroupId}", id);
                throw;
            }
        }

        public async Task<StudentGroupDto> CreateGroupAsync(CreateStudentGroupDto createGroupDto)
        {
            try
            {
                // Verify course exists
                var course = await _context.Courses
                    .Include(c => c.Faculty)
                    .FirstOrDefaultAsync(c => c.Id == createGroupDto.CourseId && c.IsActive);

                if (course == null)
                {
                    throw new InvalidOperationException($"Course with ID '{createGroupDto.CourseId}' not found.");
                }

                // Check if group code already exists in the same course
                var existingGroup = await _context.StudentGroups
                    .FirstOrDefaultAsync(g => g.Code == createGroupDto.Code &&
                                           g.CourseId == createGroupDto.CourseId &&
                                           g.IsActive);

                if (existingGroup != null)
                {
                    throw new InvalidOperationException($"Group with code '{createGroupDto.Code}' already exists in this course.");
                }

                var group = new StudentGroup
                {
                    Name = createGroupDto.Name,
                    Code = createGroupDto.Code,
                    Description = createGroupDto.Description,
                    CourseId = createGroupDto.CourseId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.StudentGroups.Add(group);
                await _context.SaveChangesAsync();

                // Use CommunityService to create the community
                try
                {
                    await _communityService.GetOrCreateGroupCommunityAsync(group.Id);
                    _logger.LogInformation("Automatically created community for group: {GroupName}", group.Name);
                }
                catch (Exception commEx)
                {
                    _logger.LogError(commEx, "Failed to automatically create community for group: {GroupName}", group.Name);
                    // Don't throw here - we don't want group creation to fail if community creation fails
                }

                // Reload with course and faculty information
                await _context.Entry(group)
                    .Reference(g => g.Course)
                    .LoadAsync();

                await _context.Entry(group.Course)
                    .Reference(c => c.Faculty)
                    .LoadAsync();

                return new StudentGroupDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    Code = group.Code,
                    Description = group.Description,
                    CourseId = group.CourseId,
                    CourseName = group.Course.Name,
                    FacultyId = group.Course.FacultyId,
                    FacultyName = group.Course.Faculty.Name,
                    CreatedAt = group.CreatedAt,
                    IsActive = group.IsActive,
                    StudentCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group: {GroupName}", createGroupDto.Name);
                throw;
            }
        }
        public async Task<StudentGroupDto> UpdateGroupAsync(string id, CreateStudentGroupDto updateGroupDto)
        {
            try
            {
                var group = await _context.StudentGroups
                    .Include(g => g.Course)
                    .ThenInclude(c => c.Faculty)
                    .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);

                if (group == null)
                {
                    throw new KeyNotFoundException($"Group with ID '{id}' not found.");
                }

                // Verify course exists if changing course
                if (group.CourseId != updateGroupDto.CourseId)
                {
                    var course = await _context.Courses
                        .FirstOrDefaultAsync(c => c.Id == updateGroupDto.CourseId && c.IsActive);

                    if (course == null)
                    {
                        throw new InvalidOperationException($"Course with ID '{updateGroupDto.CourseId}' not found.");
                    }
                }

                // Check if code is being changed and if it conflicts
                if (group.Code != updateGroupDto.Code || group.CourseId != updateGroupDto.CourseId)
                {
                    var existingGroup = await _context.StudentGroups
                        .FirstOrDefaultAsync(g => g.Code == updateGroupDto.Code &&
                                               g.CourseId == updateGroupDto.CourseId &&
                                               g.Id != id &&
                                               g.IsActive);

                    if (existingGroup != null)
                    {
                        throw new InvalidOperationException($"Group with code '{updateGroupDto.Code}' already exists in this course.");
                    }
                }

                group.Name = updateGroupDto.Name;
                group.Code = updateGroupDto.Code;
                group.Description = updateGroupDto.Description;
                group.CourseId = updateGroupDto.CourseId;

                await _context.SaveChangesAsync();

                return new StudentGroupDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    Code = group.Code,
                    Description = group.Description,
                    CourseId = group.CourseId,
                    CourseName = group.Course.Name,
                    FacultyId = group.Course.FacultyId,
                    FacultyName = group.Course.Faculty.Name,
                    CreatedAt = group.CreatedAt,
                    IsActive = group.IsActive,
                    StudentCount = group.Students.Count(s => s.IsActive)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group: {GroupId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteGroupAsync(string id)
        {
            try
            {
                var group = await _context.StudentGroups
                    .Include(g => g.Students)
                    .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);

                if (group == null)
                {
                    throw new KeyNotFoundException($"Group with ID '{id}' not found.");
                }

                // Check if group has students
                if (group.Students.Any(s => s.IsActive))
                {
                    throw new InvalidOperationException("Cannot delete group that has active students. Please reassign the students first.");
                }

                // Soft delete
                group.IsActive = false;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group: {GroupId}", id);
                throw;
            }
        }

        // ========== HELPER METHODS ==========
        public async Task<List<CourseDto>> GetCoursesForDropdownAsync()
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.Faculty)
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Faculty.Name)
                    .ThenBy(c => c.Year)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                return courses.Select(c => new CourseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Year = c.Year,
                    Code = c.Code,
                    FacultyId = c.FacultyId,
                    FacultyName = c.Faculty.Name,
                    CreatedAt = c.CreatedAt,
                    IsActive = c.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses for dropdown");
                throw;
            }
        }

        public async Task<List<StudentGroupDto>> GetGroupsForDropdownAsync()
        {
            try
            {
                var groups = await _context.StudentGroups
                    .Include(g => g.Course)
                    .ThenInclude(c => c.Faculty)
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.Course.Faculty.Name)
                    .ThenBy(g => g.Course.Name)
                    .ThenBy(g => g.Name)
                    .ToListAsync();

                return groups.Select(g => new StudentGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Code = g.Code,
                    CourseId = g.CourseId,
                    CourseName = g.Course.Name,
                    FacultyId = g.Course.FacultyId,
                    FacultyName = g.Course.Faculty.Name,
                    CreatedAt = g.CreatedAt,
                    IsActive = g.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups for dropdown");
                throw;
            }
        }

    }
}
