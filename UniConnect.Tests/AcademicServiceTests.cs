using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Maping;
using UniConnect.Models;
using UniConnect.Repository;

namespace UniConnect.Tests;

public class AcademicServiceTests
{
    private static ApplicationDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static AcademicService CreateService(ApplicationDbContext context, ICommunityService? communityService = null)
    {
        communityService ??= new FakeCommunityService();
        return new AcademicService(context, communityService, NullLogger<AcademicService>.Instance);
    }

    private static ApplicationUser CreateDean(string email)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            FirstName = "Dean",
            LastName = "User",
            Role = UserRole.Dean,
            IsActive = true
        };
    }

    [Fact]
    public async Task CreateFacultyAsync_RequiresDeanId()
    {
        using var context = CreateDbContext(nameof(CreateFacultyAsync_RequiresDeanId));
        var service = CreateService(context);

        var dto = new CreateFacultyDto
        {
            Name = "Engineering",
            Code = "ENG",
            Description = "Engineering Faculty",
            DeanId = null
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateFacultyAsync(dto));
    }

    [Fact]
    public async Task CreateFacultyCourseGroup_HappyPath()
    {
        using var context = CreateDbContext(nameof(CreateFacultyCourseGroup_HappyPath));
        var dean = CreateDean("dean1@unic.test");
        context.Users.Add(dean);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var faculty = await service.CreateFacultyAsync(new CreateFacultyDto
        {
            Name = "Engineering",
            Code = "ENG",
            Description = "Engineering Faculty",
            DeanId = dean.Id
        });

        var course = await service.CreateCourseAsync(new CreateCourseDto
        {
            Name = "Software Engineering",
            Year = 3,
            Code = "SE",
            FacultyId = faculty.Id
        });

        var group = await service.CreateGroupAsync(new CreateStudentGroupDto
        {
            Name = "Group A",
            Code = "SE3-A",
            Description = "Third year group A",
            CourseId = course.Id
        });

        Assert.Equal(faculty.Id, course.FacultyId);
        Assert.Equal(course.Id, group.CourseId);
        Assert.Equal("Engineering", faculty.Name);
        Assert.Equal("Software Engineering", course.Name);
        Assert.Equal("Group A", group.Name);
    }

    [Fact]
    public async Task GetByDeanFiltersWork()
    {
        using var context = CreateDbContext(nameof(GetByDeanFiltersWork));
        var deanA = CreateDean("deanA@unic.test");
        var deanB = CreateDean("deanB@unic.test");
        context.Users.AddRange(deanA, deanB);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var facultyA = await service.CreateFacultyAsync(new CreateFacultyDto
        {
            Name = "Engineering",
            Code = "ENG",
            DeanId = deanA.Id
        });

        var facultyB = await service.CreateFacultyAsync(new CreateFacultyDto
        {
            Name = "Medicine",
            Code = "MED",
            DeanId = deanB.Id
        });

        var courseA = await service.CreateCourseAsync(new CreateCourseDto
        {
            Name = "Software Engineering",
            Year = 3,
            Code = "SE",
            FacultyId = facultyA.Id
        });

        var courseB = await service.CreateCourseAsync(new CreateCourseDto
        {
            Name = "Anatomy",
            Year = 2,
            Code = "AN",
            FacultyId = facultyB.Id
        });

        var groupA = await service.CreateGroupAsync(new CreateStudentGroupDto
        {
            Name = "Group A",
            Code = "SE3-A",
            CourseId = courseA.Id
        });

        var groupB = await service.CreateGroupAsync(new CreateStudentGroupDto
        {
            Name = "Group B",
            Code = "AN2-B",
            CourseId = courseB.Id
        });

        var facultiesForA = await service.GetFacultiesByDeanAsync(deanA.Id);
        var coursesForA = await service.GetCoursesByDeanAsync(deanA.Id);
        var groupsForA = await service.GetGroupsByDeanAsync(deanA.Id);

        Assert.Single(facultiesForA);
        Assert.Single(coursesForA);
        Assert.Single(groupsForA);

        Assert.Equal(facultyA.Id, facultiesForA[0].Id);
        Assert.Equal(courseA.Id, coursesForA[0].Id);
        Assert.Equal(groupA.Id, groupsForA[0].Id);
    }

    [Fact]
    public async Task DeleteCourseAsync_RejectsWhenActiveGroupsExist()
    {
        using var context = CreateDbContext(nameof(DeleteCourseAsync_RejectsWhenActiveGroupsExist));
        var dean = CreateDean("dean2@unic.test");
        context.Users.Add(dean);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var faculty = await service.CreateFacultyAsync(new CreateFacultyDto
        {
            Name = "Engineering",
            Code = "ENG2",
            DeanId = dean.Id
        });

        var course = await service.CreateCourseAsync(new CreateCourseDto
        {
            Name = "Data Science",
            Year = 2,
            Code = "DS",
            FacultyId = faculty.Id
        });

        await service.CreateGroupAsync(new CreateStudentGroupDto
        {
            Name = "Group 1",
            Code = "DS2-1",
            CourseId = course.Id
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteCourseAsync(course.Id));
    }

    private sealed class FakeCommunityService : ICommunityService
    {
        private static CommunityDto CreateCommunity(string id, string name) => new CommunityDto
        {
            Id = id,
            Name = name,
            Type = CommunityType.Department,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        public Task<List<CommunityDto>> GetAllCommunitiesAsync() => Task.FromResult(new List<CommunityDto>());
        public Task<CommunityDto> GetCommunityByIdAsync(string id) => Task.FromResult(CreateCommunity(id, "Community"));
        public Task<CommunityDto> UpdateCommunityAsync(string id, UpdateCommunityDto updateCommunityDto) => Task.FromResult(CreateCommunity(id, "Community"));
        public Task<bool> DeleteCommunityAsync(string id) => Task.FromResult(true);
        public Task<bool> JoinCommunityAsync(string communityId, string userId) => Task.FromResult(true);
        public Task<bool> LeaveCommunityAsync(string communityId, string userId) => Task.FromResult(true);
        public Task<List<CommunityMemberDto>> GetCommunityMembersAsync(string communityId) => Task.FromResult(new List<CommunityMemberDto>());
        public Task<List<CommunityDto>> GetUserCommunitiesAsync(string userEmail) => Task.FromResult(new List<CommunityDto>());
        public Task<List<CommunityDto>> GetOwnedCommunitiesAsync(string userId) => Task.FromResult(new List<CommunityDto>());

        public Task<CommunityDto> GetOrCreateFacultyCommunityAsync(string facultyId) => Task.FromResult(CreateCommunity(facultyId, "Faculty Community"));
        public Task<CommunityDto> GetOrCreateCourseCommunityAsync(string courseId) => Task.FromResult(CreateCommunity(courseId, "Course Community"));
        public Task<CommunityDto> GetOrCreateGroupCommunityAsync(string groupId) => Task.FromResult(CreateCommunity(groupId, "Group Community"));
        public Task<CommunityDto> GetOrCreateSubjectCommunityAsync(string subjectId) => Task.FromResult(CreateCommunity(subjectId, "Subject Community"));
        public Task<CommunityDto> CreateDepartmentCommunityAsync(CreateDepartmentCommunityDto createDepartmentDto) => Task.FromResult(CreateCommunity(Guid.NewGuid().ToString(), createDepartmentDto.Name));

        public Task<CommunityInvitationDto> CreateInvitationAsync(string communityId, string inviterId, string inviteeEmail) => Task.FromResult(new CommunityInvitationDto());
        public Task<List<CommunityInvitationDto>> GetInvitationsForEmailAsync(string inviteeEmail) => Task.FromResult(new List<CommunityInvitationDto>());
        public Task<CommunityInvitationDto> RespondToInvitationAsync(string invitationId, string inviteeEmail, bool accept) => Task.FromResult(new CommunityInvitationDto());
    }
}

public class CommunityServiceTests
{
    private static (ApplicationDbContext context, CommunityService service) CreateServiceWithUser(ApplicationUser user)
    {
        var dbName = $"CommunityServiceTests_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Users.Add(user);
        context.SaveChanges();

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim(ClaimTypes.NameIdentifier, user.Id)
                }, "TestAuth"))
            }
        };

        var service = new CommunityService(
            context,
            NullLogger<CommunityService>.Instance,
            httpContextAccessor);

        return (context, service);
    }

    [Fact]
    public async Task CreateDepartmentCommunity_AllowsDepartmentManager()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "dm@unic.test",
            UserName = "dm@unic.test",
            FirstName = "Dept",
            LastName = "Manager",
            Role = UserRole.DepartmentManager,
            IsActive = true
        };

        var (context, service) = CreateServiceWithUser(user);

        var dto = new CreateDepartmentCommunityDto
        {
            Name = "Engineering Department",
            Description = "Department community",
            AllowPosts = true,
            AutoJoin = false
        };

        var result = await service.CreateDepartmentCommunityAsync(dto);

        Assert.Equal("Engineering Department", result.Name);
        Assert.Equal(CommunityType.Department, result.Type);
    }

    [Fact]
    public async Task CreateDepartmentCommunity_BlocksNonDepartmentManager()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "student@unic.test",
            UserName = "student@unic.test",
            FirstName = "Student",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var (context, service) = CreateServiceWithUser(user);

        var dto = new CreateDepartmentCommunityDto
        {
            Name = "General Department",
            Description = "Department community",
            AllowPosts = true,
            AutoJoin = false
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateDepartmentCommunityAsync(dto));
    }

    [Fact]
    public async Task JoinCommunity_AllowsAnyAuthenticatedUser()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "student2@unic.test",
            UserName = "student2@unic.test",
            FirstName = "Student",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var (context, service) = CreateServiceWithUser(user);

        var community = new Community
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Department Community",
            Description = "Department",
            Type = CommunityType.Department,
            AllowPosts = true,
            AutoJoin = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Communities.Add(community);
        await context.SaveChangesAsync();

        var joined = await service.JoinCommunityAsync(community.Id, user.Id);

        Assert.True(joined);
        Assert.True(context.CommunityMembers.Any(m => m.CommunityId == community.Id && m.UserId == user.Id && m.IsActive));
    }
}
