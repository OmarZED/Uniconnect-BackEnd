using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Maping;
using UniConnect.Models;
using UniConnect.Repository;

namespace UniConnect.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task UpdateProfileByEmailAsync_StudentRequiresCompleteAcademicAssignment()
    {
        var setup = BuildAuthService();
        var userManager = setup.UserManager;
        var authService = setup.AuthService;
        var context = setup.Context;

        var student = new ApplicationUser
        {
            UserName = "student@unic.test",
            Email = "student@unic.test",
            FirstName = "Student",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(student, "Password123!");
        Assert.True(createResult.Succeeded);

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Student",
            LastName = "User",
            FacultyId = "fac-1"
            // CourseId and StudentGroupId missing on purpose
        };

        var result = await authService.UpdateProfileByEmailAsync(student.Email!, updateDto);

        Assert.False(result.Success);
        Assert.Contains(result.Errors ?? Array.Empty<string>(), e => e.Contains("Faculty, Course, and Group", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateProfileByEmailAsync_StudentValidAssignment_TriggersCommunityJoins()
    {
        var setup = BuildAuthService();
        var userManager = setup.UserManager;
        var authService = setup.AuthService;
        var context = setup.Context;
        var communitySpy = setup.CommunityService;

        var faculty = new Faculty
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Engineering",
            Code = "ENG",
            DeanId = Guid.NewGuid().ToString(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var course = new Course
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Software Engineering",
            Code = "SE",
            Year = 3,
            FacultyId = faculty.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var group = new StudentGroup
        {
            Id = Guid.NewGuid().ToString(),
            Name = "SE3-A",
            Code = "SE3-A",
            CourseId = course.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Faculties.Add(faculty);
        context.Courses.Add(course);
        context.StudentGroups.Add(group);
        await context.SaveChangesAsync();

        var student = new ApplicationUser
        {
            UserName = "student2@unic.test",
            Email = "student2@unic.test",
            FirstName = "Student",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(student, "Password123!");
        Assert.True(createResult.Succeeded);

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Student",
            LastName = "User",
            FacultyId = faculty.Id,
            CourseId = course.Id,
            StudentGroupId = group.Id
        };

        var result = await authService.UpdateProfileByEmailAsync(student.Email!, updateDto);

        Assert.True(result.Success);
        Assert.Equal(faculty.Id, result.User?.FacultyId);
        Assert.Equal(course.Id, result.User?.CourseId);
        Assert.Equal(group.Id, result.User?.StudentGroupId);

        Assert.Equal(3, communitySpy.JoinCalls.Count);
        Assert.Contains(communitySpy.JoinCalls, c => c.CommunityId == faculty.Id && c.UserId == student.Id);
        Assert.Contains(communitySpy.JoinCalls, c => c.CommunityId == course.Id && c.UserId == student.Id);
        Assert.Contains(communitySpy.JoinCalls, c => c.CommunityId == group.Id && c.UserId == student.Id);
    }

    private static AuthServiceSetup BuildAuthService()
    {
        var services = new ServiceCollection();

        var dbName = $"AuthServiceTests_{Guid.NewGuid()}";
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager();

        services.AddHttpContextAccessor();
        services.AddAuthentication();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-key-please-change",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryInMinutes"] = "60"
            })
            .Build();

        services.AddSingleton<IConfiguration>(config);

        var communitySpy = new CommunityServiceSpy();
        services.AddSingleton<ICommunityService>(communitySpy);

        services.AddLogging();

        var provider = services.BuildServiceProvider();

        var context = provider.GetRequiredService<ApplicationDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = provider.GetRequiredService<SignInManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        var authService = new AuthService(
            userManager,
            signInManager,
            roleManager,
            config,
            NullLogger<AuthService>.Instance,
            context,
            communitySpy);

        return new AuthServiceSetup(context, userManager, authService, communitySpy);
    }

    private sealed record AuthServiceSetup(
        ApplicationDbContext Context,
        UserManager<ApplicationUser> UserManager,
        AuthService AuthService,
        CommunityServiceSpy CommunityService);

    private sealed class CommunityServiceSpy : ICommunityService
    {
        public record JoinCall(string CommunityId, string UserId);
        public List<JoinCall> JoinCalls { get; } = new();

        public Task<List<CommunityDto>> GetAllCommunitiesAsync() => Task.FromResult(new List<CommunityDto>());
        public Task<CommunityDto> GetCommunityByIdAsync(string id) => Task.FromResult(new CommunityDto());
        public Task<CommunityDto> UpdateCommunityAsync(string id, UpdateCommunityDto updateCommunityDto) => Task.FromResult(new CommunityDto());
        public Task<bool> DeleteCommunityAsync(string id) => Task.FromResult(true);
        public Task<bool> LeaveCommunityAsync(string communityId, string userId) => Task.FromResult(true);
        public Task<List<CommunityMemberDto>> GetCommunityMembersAsync(string communityId) => Task.FromResult(new List<CommunityMemberDto>());
        public Task<List<CommunityDto>> GetUserCommunitiesAsync(string userEmail) => Task.FromResult(new List<CommunityDto>());

        public Task<CommunityDto> GetOrCreateFacultyCommunityAsync(string facultyId) =>
            Task.FromResult(new CommunityDto { Id = facultyId, Name = "Faculty", Type = CommunityType.Faculty, CreatedAt = DateTime.UtcNow, IsActive = true });

        public Task<CommunityDto> GetOrCreateCourseCommunityAsync(string courseId) =>
            Task.FromResult(new CommunityDto { Id = courseId, Name = "Course", Type = CommunityType.Course, CreatedAt = DateTime.UtcNow, IsActive = true });

        public Task<CommunityDto> GetOrCreateGroupCommunityAsync(string groupId) =>
            Task.FromResult(new CommunityDto { Id = groupId, Name = "Group", Type = CommunityType.Group, CreatedAt = DateTime.UtcNow, IsActive = true });

        public Task<CommunityDto> CreateDepartmentCommunityAsync(CreateDepartmentCommunityDto createDepartmentDto) =>
            Task.FromResult(new CommunityDto { Id = Guid.NewGuid().ToString(), Name = createDepartmentDto.Name, Type = CommunityType.Department, CreatedAt = DateTime.UtcNow, IsActive = true });

        public Task<bool> JoinCommunityAsync(string communityId, string userId)
        {
            JoinCalls.Add(new JoinCall(communityId, userId));
            return Task.FromResult(true);
        }

        public Task<CommunityInvitationDto> CreateInvitationAsync(string communityId, string inviterId, string inviteeEmail) =>
            Task.FromResult(new CommunityInvitationDto());
        public Task<List<CommunityInvitationDto>> GetInvitationsForEmailAsync(string inviteeEmail) =>
            Task.FromResult(new List<CommunityInvitationDto>());
        public Task<CommunityInvitationDto> RespondToInvitationAsync(string invitationId, string inviteeEmail, bool accept) =>
            Task.FromResult(new CommunityInvitationDto());
    }
}
