using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Maping;
using UniConnect.Models;

namespace UniConnect.Repository
{
    public class CommunityService : ICommunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommunityService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommunityService(
            ApplicationDbContext context,
            ILogger<CommunityService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        // Community CRUD
        public async Task<List<CommunityDto>> GetAllCommunitiesAsync()
        {
            try
            {
                var communities = await _context.Communities
                    .Include(c => c.Faculty)
                    .Include(c => c.Course)
                    .Include(c => c.StudentGroup)
                    .Include(c => c.Members)
                    .Include(c => c.Posts) // ADDED THIS - CRITICAL
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Type)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                return communities.Select(c => MapToCommunityDto(c)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all communities");
                throw;
            }
        }

        public async Task<CommunityDto> GetCommunityByIdAsync(string id)
        {
            try
            {
                var community = await _context.Communities
                    .Include(c => c.Faculty)
                    .Include(c => c.Course)
                    .Include(c => c.StudentGroup)
                    .Include(c => c.Members)
                    .Include(c => c.Posts) // ADDED THIS - CRITICAL
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (community == null) return null;

                // Get current user ID for member status
                var currentUserId = await GetCurrentUserIdAsync();

                return MapToCommunityDto(community, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community by ID: {CommunityId}", id);
                throw;
            }
        }

        public async Task<CommunityDto> CreateDepartmentCommunityAsync(CreateDepartmentCommunityDto createDepartmentDto)
        {
            try
            {
                _logger.LogInformation("🎯 Creating department community: {Name}", createDepartmentDto.Name);

                // Get current user
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new UnauthorizedAccessException("User must be authenticated to create a community.");
                }

                _logger.LogInformation("👤 Creating community with user: {UserId} - {Email}", currentUser.Id, currentUser.Email);

                // Create community
                var community = new Community
                {
                    Name = createDepartmentDto.Name,
                    Description = createDepartmentDto.Description,
                    Type = CommunityType.Department,
                    FacultyId = null,
                    CourseId = null,
                    StudentGroupId = null,
                    AllowPosts = createDepartmentDto.AllowPosts,
                    AutoJoin = createDepartmentDto.AutoJoin,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Communities.Add(community);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Community created with ID: {CommunityId}", community.Id);

                // Add creator as admin
                var creatorMember = new CommunityMember
                {
                    CommunityId = community.Id,
                    UserId = currentUser.Id,
                    Role = CommunityRole.Admin,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.CommunityMembers.Add(creatorMember);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Community member (admin) added for user: {UserId}", currentUser.Id);

                return MapToCommunityDto(community, currentUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating department community: {CommunityName}", createDepartmentDto.Name);
                throw;
            }
        }

        public async Task<CommunityDto> UpdateCommunityAsync(string id, UpdateCommunityDto updateCommunityDto)
        {
            try
            {
                var community = await _context.Communities
                    .Include(c => c.Faculty)
                    .Include(c => c.Course)
                    .Include(c => c.StudentGroup)
                    .Include(c => c.Posts) // ADDED THIS
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (community == null)
                {
                    throw new KeyNotFoundException($"Community with ID '{id}' not found.");
                }

                // Prevent updating academic relationships for academic communities
                if (community.Type != CommunityType.Department)
                {
                    throw new InvalidOperationException(
                        $"Cannot update academic relationships for {community.Type} communities. " +
                        "Update the academic entity instead.");
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateCommunityDto.Name))
                    community.Name = updateCommunityDto.Name;

                if (!string.IsNullOrEmpty(updateCommunityDto.Description))
                    community.Description = updateCommunityDto.Description;

                if (updateCommunityDto.AllowPosts.HasValue)
                    community.AllowPosts = updateCommunityDto.AllowPosts.Value;

                if (updateCommunityDto.AutoJoin.HasValue)
                    community.AutoJoin = updateCommunityDto.AutoJoin.Value;

                community.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Get current user ID for member status
                var currentUserId = await GetCurrentUserIdAsync();

                return MapToCommunityDto(community, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating community: {CommunityId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCommunityAsync(string id)
        {
            try
            {
                var community = await _context.Communities
                    .Include(c => c.Posts)
                    .Include(c => c.Members)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (community == null)
                {
                    throw new KeyNotFoundException($"Community with ID '{id}' not found.");
                }

                // For academic communities, prevent deletion if they have academic relationships
                if (community.Type != CommunityType.Department &&
                    (community.FacultyId != null || community.CourseId != null || community.StudentGroupId != null))
                {
                    throw new InvalidOperationException("Cannot delete academic communities. Delete the academic entity instead.");
                }

                // Soft delete
                community.IsActive = false;
                community.UpdatedAt = DateTime.UtcNow;

                // Also deactivate all members
                foreach (var member in community.Members.Where(m => m.IsActive))
                {
                    member.IsActive = false;
                    member.LeftAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting community: {CommunityId}", id);
                throw;
            }
        }

        // Community membership
        public async Task<bool> JoinCommunityAsync(string communityId, string userId)
        {
            try
            {
                var community = await _context.Communities
                    .FirstOrDefaultAsync(c => c.Id == communityId && c.IsActive);

                if (community == null)
                {
                    throw new KeyNotFoundException($"Community with ID '{communityId}' not found.");
                }

                // Check if user exists
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    throw new InvalidOperationException($"User with ID '{userId}' not found.");
                }

                // Check if user is already a member
                var existingMember = await _context.CommunityMembers
                    .FirstOrDefaultAsync(cm => cm.CommunityId == communityId && cm.UserId == userId);

                if (existingMember != null)
                {
                    if (existingMember.IsActive)
                    {
                        throw new InvalidOperationException("User is already a member of this community.");
                    }
                    else
                    {
                        // Reactivate existing membership
                        existingMember.IsActive = true;
                        existingMember.LeftAt = null;
                        existingMember.JoinedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // Create new membership
                    var member = new CommunityMember
                    {
                        CommunityId = communityId,
                        UserId = userId,
                        Role = CommunityRole.Member,
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.CommunityMembers.Add(member);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining community {CommunityId} for user {UserId}", communityId, userId);
                throw;
            }
        }

        public async Task<bool> LeaveCommunityAsync(string communityId, string userId)
        {
            try
            {
                var member = await _context.CommunityMembers
                    .FirstOrDefaultAsync(cm => cm.CommunityId == communityId && cm.UserId == userId && cm.IsActive);

                if (member == null)
                {
                    throw new InvalidOperationException("User is not a member of this community.");
                }

                // Check if user is the last admin
                if (member.Role == CommunityRole.Admin)
                {
                    var adminCount = await _context.CommunityMembers
                        .CountAsync(cm => cm.CommunityId == communityId &&
                                         cm.Role == CommunityRole.Admin &&
                                         cm.IsActive);

                    if (adminCount <= 1)
                    {
                        throw new InvalidOperationException("Cannot leave community as the last admin. Assign another admin first.");
                    }
                }

                // For academic communities with auto-join, we just deactivate instead of remove
                var community = await _context.Communities.FindAsync(communityId);
                if (community?.AutoJoin == true && community.Type != CommunityType.Department)
                {
                    member.IsActive = false;
                    member.LeftAt = DateTime.UtcNow;
                }
                else
                {
                    // For manual join communities, remove completely
                    _context.CommunityMembers.Remove(member);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving community {CommunityId} for user {UserId}", communityId, userId);
                throw;
            }
        }

        public async Task<List<CommunityMemberDto>> GetCommunityMembersAsync(string communityId)
        {
            try
            {
                var members = await _context.CommunityMembers
                    .Include(cm => cm.User)
                    .Where(cm => cm.CommunityId == communityId && cm.IsActive)
                    .OrderByDescending(cm => cm.Role) // Admins first, then moderators, then members
                    .ThenBy(cm => cm.User.FirstName)
                    .ThenBy(cm => cm.User.LastName)
                    .ToListAsync();

                return members.Select(m => new CommunityMemberDto
                {
                    Id = m.Id,
                    CommunityId = m.CommunityId,
                    UserId = m.UserId,
                    UserEmail = m.User.Email,
                    UserFirstName = m.User.FirstName,
                    UserLastName = m.User.LastName,
                    UserFullName = $"{m.User.FirstName} {m.User.LastName}",
                    ProfilePictureUrl = m.User.ProfilePictureUrl,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt,
                    IsActive = m.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting members for community: {CommunityId}", communityId);
                throw;
            }
        }

        // Automatic community creation for academic structures
        public async Task<CommunityDto> GetOrCreateFacultyCommunityAsync(string facultyId)
        {
            try
            {
                var faculty = await _context.Faculties
                    .FirstOrDefaultAsync(f => f.Id == facultyId && f.IsActive);

                if (faculty == null)
                {
                    throw new KeyNotFoundException($"Faculty with ID '{facultyId}' not found.");
                }

                // Check if community already exists
                var existingCommunity = await _context.Communities
                    .Include(c => c.Posts) // ADDED THIS
                    .FirstOrDefaultAsync(c => c.FacultyId == facultyId && c.Type == CommunityType.Faculty && c.IsActive);

                if (existingCommunity != null)
                {
                    return MapToCommunityDto(existingCommunity);
                }

                // Get current user for admin
                var currentUser = await GetCurrentUserAsync();
                var adminUserId = currentUser?.Id ?? "system";

                // Create new faculty community
                var community = new Community
                {
                    Name = $"{faculty.Name} Community",
                    Description = $"Official community for {faculty.Name}",
                    Type = CommunityType.Faculty,
                    FacultyId = facultyId,
                    AllowPosts = true,
                    AutoJoin = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Communities.Add(community);
                await _context.SaveChangesAsync();

                // Add admin
                var creatorMember = new CommunityMember
                {
                    CommunityId = community.Id,
                    UserId = adminUserId,
                    Role = CommunityRole.Admin,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.CommunityMembers.Add(creatorMember);
                await _context.SaveChangesAsync();

                return MapToCommunityDto(community);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting/creating faculty community for faculty: {FacultyId}", facultyId);
                throw;
            }
        }

        public async Task<CommunityDto> GetOrCreateCourseCommunityAsync(string courseId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Faculty)
                    .FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive);

                if (course == null)
                {
                    throw new KeyNotFoundException($"Course with ID '{courseId}' not found.");
                }

                // Check if community already exists
                var existingCommunity = await _context.Communities
                    .Include(c => c.Posts) // ADDED THIS
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.Type == CommunityType.Course && c.IsActive);

                if (existingCommunity != null)
                {
                    return MapToCommunityDto(existingCommunity);
                }

                // Get current user for admin
                var currentUser = await GetCurrentUserAsync();
                var adminUserId = currentUser?.Id ?? "system";

                // Create new course community
                var community = new Community
                {
                    Name = $"{course.Name} - {course.Faculty.Name}",
                    Description = $"Course community for {course.Name}",
                    Type = CommunityType.Course,
                    CourseId = courseId,
                    FacultyId = course.FacultyId,
                    AllowPosts = true,
                    AutoJoin = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Communities.Add(community);
                await _context.SaveChangesAsync();

                // Add admin
                var creatorMember = new CommunityMember
                {
                    CommunityId = community.Id,
                    UserId = adminUserId,
                    Role = CommunityRole.Admin,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.CommunityMembers.Add(creatorMember);
                await _context.SaveChangesAsync();

                return MapToCommunityDto(community);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting/creating course community for course: {CourseId}", courseId);
                throw;
            }
        }

        public async Task<CommunityDto> GetOrCreateGroupCommunityAsync(string groupId)
        {
            try
            {
                var group = await _context.StudentGroups
                    .Include(g => g.Course)
                    .ThenInclude(c => c.Faculty)
                    .FirstOrDefaultAsync(g => g.Id == groupId && g.IsActive);

                if (group == null)
                {
                    throw new KeyNotFoundException($"Student group with ID '{groupId}' not found.");
                }

                // Check if community already exists
                var existingCommunity = await _context.Communities
                    .Include(c => c.Posts) // ADDED THIS
                    .FirstOrDefaultAsync(c => c.StudentGroupId == groupId && c.Type == CommunityType.Group && c.IsActive);

                if (existingCommunity != null)
                {
                    return MapToCommunityDto(existingCommunity);
                }

                // Get current user for admin
                var currentUser = await GetCurrentUserAsync();
                var adminUserId = currentUser?.Id ?? "system";

                // Create new group community
                var community = new Community
                {
                    Name = $"{group.Name} - {group.Course.Name}",
                    Description = $"Group community for {group.Name}",
                    Type = CommunityType.Group,
                    StudentGroupId = groupId,
                    CourseId = group.CourseId,
                    FacultyId = group.Course.FacultyId,
                    AllowPosts = true,
                    AutoJoin = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Communities.Add(community);
                await _context.SaveChangesAsync();

                // Add admin
                var creatorMember = new CommunityMember
                {
                    CommunityId = community.Id,
                    UserId = adminUserId,
                    Role = CommunityRole.Admin,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.CommunityMembers.Add(creatorMember);
                await _context.SaveChangesAsync();

                return MapToCommunityDto(community);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting/creating group community for group: {GroupId}", groupId);
                throw;
            }
        }

        // User-specific methods
        public async Task<List<CommunityDto>> GetUserCommunitiesAsync(string userEmail)
        {
            try
            {
                _logger.LogInformation("🔍 Getting communities for user: {UserEmail}", userEmail);

                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                {
                    _logger.LogWarning("User not found with email: {UserEmail}", userEmail);
                    return new List<CommunityDto>();
                }

                // Get user memberships
                var userMemberships = await _context.CommunityMembers
                    .Include(cm => cm.Community)
                        .ThenInclude(c => c.Faculty)
                    .Include(cm => cm.Community)
                        .ThenInclude(c => c.Course)
                    .Include(cm => cm.Community)
                        .ThenInclude(c => c.StudentGroup)
                    .Include(cm => cm.Community)
                        .ThenInclude(c => c.Members)
                    .Include(cm => cm.Community)
                        .ThenInclude(c => c.Posts) // ADDED THIS
                    .Where(cm => cm.UserId == user.Id && cm.IsActive && cm.Community.IsActive)
                    .OrderBy(cm => cm.Community.Type)
                    .ThenBy(cm => cm.Community.Name)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} active memberships for user {UserEmail}", userMemberships.Count, userEmail);

                var communities = userMemberships.Select(m =>
                {
                    var community = m.Community;
                    return new CommunityDto
                    {
                        Id = community.Id,
                        Name = community.Name,
                        Description = community.Description,
                        Type = community.Type,
                        FacultyId = community.FacultyId,
                        FacultyName = community.Faculty?.Name,
                        CourseId = community.CourseId,
                        CourseName = community.Course?.Name,
                        StudentGroupId = community.StudentGroupId,
                        StudentGroupName = community.StudentGroup?.Name,
                        MemberCount = community.Members.Count(m => m.IsActive),
                        PostCount = community.Posts.Count(p => p.IsActive),
                        AllowPosts = community.AllowPosts,
                        AutoJoin = community.AutoJoin,
                        CreatedAt = community.CreatedAt,
                        UpdatedAt = community.UpdatedAt,
                        IsActive = community.IsActive,
                        CurrentUserRole = m.Role,
                        IsCurrentUserMember = true
                    };
                }).ToList();

                return communities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user communities for user: {UserEmail}", userEmail);
                throw;
            }
        }

        // Helper methods
        private CommunityDto MapToCommunityDto(Community community, string? currentUserId = null)
        {
            var dto = new CommunityDto
            {
                Id = community.Id,
                Name = community.Name,
                Description = community.Description,
                Type = community.Type,
                FacultyId = community.FacultyId,
                FacultyName = community.Faculty?.Name,
                CourseId = community.CourseId,
                CourseName = community.Course?.Name,
                StudentGroupId = community.StudentGroupId,
                StudentGroupName = community.StudentGroup?.Name,
                MemberCount = community.Members?.Count(m => m.IsActive) ?? 0,
                PostCount = community.Posts?.Count(p => p.IsActive) ?? 0,
                AllowPosts = community.AllowPosts,
                AutoJoin = community.AutoJoin,
                CreatedAt = community.CreatedAt,
                UpdatedAt = community.UpdatedAt,
                IsActive = community.IsActive
            };

            // Set current user's role if userId is provided and community has members
            if (!string.IsNullOrEmpty(currentUserId) && community.Members != null)
            {
                var userMembership = community.Members
                    .FirstOrDefault(m => m.UserId == currentUserId && m.IsActive);

                if (userMembership != null)
                {
                    dto.CurrentUserRole = userMembership.Role;
                    dto.IsCurrentUserMember = true;
                }
                else
                {
                    dto.CurrentUserRole = null;
                    dto.IsCurrentUserMember = false;
                }
            }

            return dto;
        }

        // Helper to get current user
        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // Try to get user by email
            var userEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                // Try NameIdentifier (might be email)
                userEmail = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(userEmail))
            {
                // Try custom claim
                userEmail = httpContext.User.FindFirst("email")?.Value;
            }

            if (!string.IsNullOrEmpty(userEmail))
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            }

            return null;
        }

        // Helper to get current user ID
        private async Task<string?> GetCurrentUserIdAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Id;
        }
    }
}