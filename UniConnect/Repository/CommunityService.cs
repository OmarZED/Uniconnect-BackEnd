using Microsoft.EntityFrameworkCore;
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


        public CommunityService(ApplicationDbContext context, ILogger<CommunityService> logger)
        {
            _context = context;
            _logger = logger;
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
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (community == null) return null;

                return MapToCommunityDto(community);
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
                // For now, we'll use "system" as creator until we implement proper auth context
                var creatorId = "system"; // This should be the actual user ID from auth context

                var community = new Community
                {
                    Name = createDepartmentDto.Name,
                    Description = createDepartmentDto.Description,
                    Type = CommunityType.Department,
                    FacultyId = null, // Explicitly set to null
                    CourseId = null,   // Explicitly set to null
                    StudentGroupId = null, // Explicitly set to null
                    AllowPosts = createDepartmentDto.AllowPosts,
                    AutoJoin = createDepartmentDto.AutoJoin,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Communities.Add(community);
                await _context.SaveChangesAsync();

                // Add creator as admin
                var creatorMember = new CommunityMember
                {
                    CommunityId = community.Id,
                    UserId = creatorId,
                    Role = CommunityRole.Admin, // Creator becomes admin
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.CommunityMembers.Add(creatorMember);
                await _context.SaveChangesAsync();

                return MapToCommunityDto(community);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department community: {CommunityName}", createDepartmentDto.Name);
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

                return MapToCommunityDto(community);
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

        // Automatic community creation for academic structures - FIXED VERSIONS
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
                    .FirstOrDefaultAsync(c => c.FacultyId == facultyId && c.Type == CommunityType.Faculty && c.IsActive);

                if (existingCommunity != null)
                {
                    return MapToCommunityDto(existingCommunity);
                }

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
                await _context.SaveChangesAsync(); // ✅ Save community first to get ID

                // Add system as admin - NOW community.Id has the actual value
                var creatorMember = new CommunityMember
                {
                    CommunityId = community.Id, // ✅ Now this is the actual ID
                    UserId = "system",
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
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.Type == CommunityType.Course && c.IsActive);

                if (existingCommunity != null)
                {
                    return MapToCommunityDto(existingCommunity);
                }

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
                await _context.SaveChangesAsync(); // ✅ Save community first to get ID

                // Add system as admin - NOW community.Id has the actual value
                var creatorMember = new CommunityMember
                {
                    CommunityId = community.Id, // ✅ Now this is the actual ID
                    UserId = "system",
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
                    .FirstOrDefaultAsync(c => c.StudentGroupId == groupId && c.Type == CommunityType.Group && c.IsActive);

                if (existingCommunity != null)
                {
                    return MapToCommunityDto(existingCommunity);
                }

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
                await _context.SaveChangesAsync(); // ✅ Save community first to get ID

                // Add system as admin - NOW community.Id has the actual value
                var creatorMember = new CommunityMember
                {
                    CommunityId = community.Id, // ✅ Now this is the actual ID
                    UserId = "system",
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
                _logger.LogInformation("🔍 === GETTING USER COMMUNITIES ===");
                _logger.LogInformation("🔍 User Email: {UserEmail}", userEmail);

                // First, find the user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                {
                    _logger.LogWarning("❌ User not found with email: {UserEmail}", userEmail);
                    return new List<CommunityDto>();
                }

                _logger.LogInformation("🔍 Found user: {UserId} - {UserEmail}", user.Id, user.Email);

                // Now get user memberships using the User ID
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
                        .ThenInclude(c => c.Posts)
                    .Where(cm => cm.UserId == user.Id && cm.IsActive && cm.Community.IsActive)
                    .OrderBy(cm => cm.Community.Type)
                    .ThenBy(cm => cm.Community.Name)
                    .ToListAsync();

                _logger.LogInformation("🔍 Found {Count} active memberships for user {UserEmail}", userMemberships.Count, userEmail);

                // Log each community found
                foreach (var membership in userMemberships)
                {
                    _logger.LogInformation("🔍 User {UserEmail} is member of: {CommunityName} (ID: {CommunityId})",
                        userEmail, membership.Community.Name, membership.Community.Id);
                }

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
                        IsActive = community.IsActive
                    };
                }).ToList();

                _logger.LogInformation("🔍 Returning {Count} communities for user {UserEmail}", communities.Count, userEmail);
                _logger.LogInformation("🔍 === COMPLETED GETTING USER COMMUNITIES ===");

                return communities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting user communities for user: {UserEmail}", userEmail);
                throw;
            }
        }

        // Helper methods
        private CommunityDto MapToCommunityDto(Community community)
        {
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
                IsActive = community.IsActive
            };
        }

      
    }
}


