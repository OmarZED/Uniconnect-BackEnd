using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Maping;
using UniConnect.Models;
using UniConnect.Repository;

namespace UniConnect.Tests;

public class SubjectServiceTests
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

    [Fact]
    public async Task CreateSubjectAsync_CreatesForGroup()
    {
        using var context = CreateDbContext(nameof(CreateSubjectAsync_CreatesForGroup));
        var service = CreateService(context);

        var deanId = Guid.NewGuid().ToString();
        var faculty = new Faculty { Name = "Engineering", Code = "ENG", DeanId = deanId, IsActive = true, CreatedAt = DateTime.UtcNow };
        var course = new Course { Name = "Software Engineering", Code = "SE", Year = 3, FacultyId = faculty.Id, IsActive = true, CreatedAt = DateTime.UtcNow };
        var group = new StudentGroup { Name = "SE3-A", Code = "SE3-A", CourseId = course.Id, IsActive = true, CreatedAt = DateTime.UtcNow };

        context.Faculties.Add(faculty);
        context.Courses.Add(course);
        context.StudentGroups.Add(group);
        await context.SaveChangesAsync();

        var subject = await service.CreateSubjectAsync(new CreateSubjectDto
        {
            Name = "Algorithms",
            Code = "ALG",
            StudentGroupId = group.Id
        });

        Assert.Equal(group.Id, subject.StudentGroupId);
        Assert.Equal(course.Id, subject.CourseId);
        Assert.Equal(faculty.Id, subject.FacultyId);
        Assert.Equal("Algorithms", subject.Name);
    }

    [Fact]
    public async Task CreateSubjectAsync_RejectsDuplicateCodeWithinGroup()
    {
        using var context = CreateDbContext(nameof(CreateSubjectAsync_RejectsDuplicateCodeWithinGroup));
        var service = CreateService(context);

        var deanId = Guid.NewGuid().ToString();
        var faculty = new Faculty { Name = "Engineering", Code = "ENG", DeanId = deanId, IsActive = true, CreatedAt = DateTime.UtcNow };
        var course = new Course { Name = "Software Engineering", Code = "SE", Year = 3, FacultyId = faculty.Id, IsActive = true, CreatedAt = DateTime.UtcNow };
        var group = new StudentGroup { Name = "SE3-A", Code = "SE3-A", CourseId = course.Id, IsActive = true, CreatedAt = DateTime.UtcNow };

        context.Faculties.Add(faculty);
        context.Courses.Add(course);
        context.StudentGroups.Add(group);
        await context.SaveChangesAsync();

        await service.CreateSubjectAsync(new CreateSubjectDto
        {
            Name = "Algorithms",
            Code = "ALG",
            StudentGroupId = group.Id
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateSubjectAsync(new CreateSubjectDto
        {
            Name = "Algorithms 2",
            Code = "ALG",
            StudentGroupId = group.Id
        }));
    }

    private sealed class FakeCommunityService : ICommunityService
    {
        public Task<List<CommunityDto>> GetAllCommunitiesAsync() => Task.FromResult(new List<CommunityDto>());
        public Task<CommunityDto> GetCommunityByIdAsync(string id) => Task.FromResult(new CommunityDto());
        public Task<CommunityDto> UpdateCommunityAsync(string id, UpdateCommunityDto updateCommunityDto) => Task.FromResult(new CommunityDto());
        public Task<bool> DeleteCommunityAsync(string id) => Task.FromResult(true);
        public Task<bool> JoinCommunityAsync(string communityId, string userId) => Task.FromResult(true);
        public Task<bool> LeaveCommunityAsync(string communityId, string userId) => Task.FromResult(true);
        public Task<List<CommunityMemberDto>> GetCommunityMembersAsync(string communityId) => Task.FromResult(new List<CommunityMemberDto>());
        public Task<List<CommunityDto>> GetUserCommunitiesAsync(string userEmail) => Task.FromResult(new List<CommunityDto>());
        public Task<List<CommunityDto>> GetOwnedCommunitiesAsync(string userId) => Task.FromResult(new List<CommunityDto>());
        public Task<CommunityDto> GetOrCreateFacultyCommunityAsync(string facultyId) => Task.FromResult(new CommunityDto());
        public Task<CommunityDto> GetOrCreateCourseCommunityAsync(string courseId) => Task.FromResult(new CommunityDto());
        public Task<CommunityDto> GetOrCreateGroupCommunityAsync(string groupId) => Task.FromResult(new CommunityDto());
        public Task<CommunityDto> GetOrCreateSubjectCommunityAsync(string subjectId) => Task.FromResult(new CommunityDto());
        public Task<CommunityDto> CreateDepartmentCommunityAsync(CreateDepartmentCommunityDto createDepartmentDto) => Task.FromResult(new CommunityDto());

        public Task<CommunityInvitationDto> CreateInvitationAsync(string communityId, string inviterId, string inviteeEmail) => Task.FromResult(new CommunityInvitationDto());
        public Task<List<CommunityInvitationDto>> GetInvitationsForEmailAsync(string inviteeEmail) => Task.FromResult(new List<CommunityInvitationDto>());
        public Task<CommunityInvitationDto> RespondToInvitationAsync(string invitationId, string inviteeEmail, bool accept) => Task.FromResult(new CommunityInvitationDto());
    }
}
