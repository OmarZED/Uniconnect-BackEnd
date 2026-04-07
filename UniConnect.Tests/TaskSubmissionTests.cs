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

public class TaskSubmissionTests
{
    private static ApplicationDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static (ApplicationDbContext context, TaskService taskService, SubmissionService submissionService) CreateServices(ApplicationUser? currentUser = null)
    {
        var dbName = $"TaskSubmissionTests_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);

        var httpContextAccessor = new HttpContextAccessor();
        if (currentUser != null)
        {
            httpContextAccessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, currentUser.Email ?? string.Empty),
                    new Claim(ClaimTypes.NameIdentifier, currentUser.Id)
                }, "TestAuth"))
            };
        }

        var communityService = new CommunityService(
            context,
            NullLogger<CommunityService>.Instance,
            httpContextAccessor);

        var taskService = new TaskService(context, NullLogger<TaskService>.Instance);
        var submissionService = new SubmissionService(context, communityService, NullLogger<SubmissionService>.Instance);

        return (context, taskService, submissionService);
    }

    private static ApplicationUser CreateTeacher(string email)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            FirstName = "Teacher",
            LastName = "User",
            Role = UserRole.Teacher,
            IsActive = true
        };
    }

    private static ApplicationUser CreateStudent(string email)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            FirstName = "Student",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };
    }

    private static Subject CreateSubject(ApplicationUser teacher)
    {
        return new Subject
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Algorithms",
            Code = "ALG",
            TeacherId = teacher.Id,
            JoinCode = "ABC123",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Community CreateSubjectCommunity(string subjectId)
    {
        return new Community
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Algorithms Subject",
            Type = CommunityType.Subject,
            SubjectId = subjectId,
            AllowPosts = true,
            AutoJoin = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateTask_AllowsSubjectOwner()
    {
        var teacher = CreateTeacher("teacher@unic.test");
        var (context, taskService, _) = CreateServices();

        context.Users.Add(teacher);
        var subject = CreateSubject(teacher);
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();

        var dto = new CreateTaskDto
        {
            Title = "Assignment 1",
            Description = "Solve problems",
            DueDate = DateTime.UtcNow.AddDays(7),
            MaxScore = 100,
            SubjectId = subject.Id
        };

        var task = await taskService.CreateTaskAsync(dto, teacher.Id);

        Assert.Equal(subject.Id, task.SubjectId);
        Assert.Equal("Assignment 1", task.Title);
    }

    [Fact]
    public async Task CreateTask_BlocksNonOwner()
    {
        var teacher = CreateTeacher("owner@unic.test");
        var otherTeacher = CreateTeacher("other@unic.test");
        var (context, taskService, _) = CreateServices();

        context.Users.AddRange(teacher, otherTeacher);
        var subject = CreateSubject(teacher);
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();

        var dto = new CreateTaskDto
        {
            Title = "Assignment 1",
            Description = "Solve problems",
            DueDate = DateTime.UtcNow.AddDays(7),
            MaxScore = 100,
            SubjectId = subject.Id
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => taskService.CreateTaskAsync(dto, otherTeacher.Id));
    }

    [Fact]
    public async Task StudentSubmission_SingleSubmitUpdatesUntilGraded()
    {
        var teacher = CreateTeacher("teacher@unic.test");
        var student = CreateStudent("student@unic.test");
        var (context, taskService, submissionService) = CreateServices();

        context.Users.AddRange(teacher, student);
        var subject = CreateSubject(teacher);
        context.Subjects.Add(subject);

        var community = CreateSubjectCommunity(subject.Id);
        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = student.Id,
            Role = CommunityRole.Member,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var task = await taskService.CreateTaskAsync(new CreateTaskDto
        {
            Title = "Assignment 1",
            Description = "Solve problems",
            DueDate = DateTime.UtcNow.AddDays(7),
            MaxScore = 100,
            SubjectId = subject.Id
        }, teacher.Id);

        var first = await submissionService.CreateSubmissionAsync(new CreateSubmissionDto
        {
            TaskId = task.Id,
            Content = "First attempt"
        }, student.Id);

        var second = await submissionService.CreateSubmissionAsync(new CreateSubmissionDto
        {
            TaskId = task.Id,
            Content = "Updated attempt"
        }, student.Id);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Updated attempt", second.Content);
        Assert.Single(context.Submissions);
    }

    [Fact]
    public async Task StudentSubmission_RequiresSubjectMembership()
    {
        var teacher = CreateTeacher("teacher@unic.test");
        var student = CreateStudent("student@unic.test");
        var (context, taskService, submissionService) = CreateServices();

        context.Users.AddRange(teacher, student);
        var subject = CreateSubject(teacher);
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();

        var task = await taskService.CreateTaskAsync(new CreateTaskDto
        {
            Title = "Assignment 1",
            Description = "Solve problems",
            DueDate = DateTime.UtcNow.AddDays(7),
            MaxScore = 100,
            SubjectId = subject.Id
        }, teacher.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => submissionService.CreateSubmissionAsync(new CreateSubmissionDto
        {
            TaskId = task.Id,
            Content = "Attempt"
        }, student.Id));
    }

    [Fact]
    public async Task TeacherCanListSubmissions_OnlyForOwnedSubject()
    {
        var teacher = CreateTeacher("teacher@unic.test");
        var otherTeacher = CreateTeacher("other@unic.test");
        var student = CreateStudent("student@unic.test");
        var (context, taskService, submissionService) = CreateServices();

        context.Users.AddRange(teacher, otherTeacher, student);
        var subject = CreateSubject(teacher);
        context.Subjects.Add(subject);

        var community = CreateSubjectCommunity(subject.Id);
        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = student.Id,
            Role = CommunityRole.Member,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var task = await taskService.CreateTaskAsync(new CreateTaskDto
        {
            Title = "Assignment 1",
            Description = "Solve problems",
            DueDate = DateTime.UtcNow.AddDays(7),
            MaxScore = 100,
            SubjectId = subject.Id
        }, teacher.Id);

        await submissionService.CreateSubmissionAsync(new CreateSubmissionDto
        {
            TaskId = task.Id,
            Content = "Attempt"
        }, student.Id);

        var submissions = await submissionService.GetSubmissionsByTaskAsync(task.Id, teacher.Id);
        Assert.Single(submissions);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => submissionService.GetSubmissionsByTaskAsync(task.Id, otherTeacher.Id));
    }

    [Fact]
    public async Task TeacherGradesSubmission_SetsStatusAndGrade()
    {
        var teacher = CreateTeacher("teacher@unic.test");
        var student = CreateStudent("student@unic.test");
        var (context, taskService, submissionService) = CreateServices();

        context.Users.AddRange(teacher, student);
        var subject = CreateSubject(teacher);
        context.Subjects.Add(subject);

        var community = CreateSubjectCommunity(subject.Id);
        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = student.Id,
            Role = CommunityRole.Member,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var task = await taskService.CreateTaskAsync(new CreateTaskDto
        {
            Title = "Assignment 1",
            Description = "Solve problems",
            DueDate = DateTime.UtcNow.AddDays(7),
            MaxScore = 100,
            SubjectId = subject.Id
        }, teacher.Id);

        var submission = await submissionService.CreateSubmissionAsync(new CreateSubmissionDto
        {
            TaskId = task.Id,
            Content = "Attempt"
        }, student.Id);

        var graded = await submissionService.GradeSubmissionAsync(submission.Id, new GradeSubmissionDto
        {
            Grade = 95,
            Feedback = "Great work"
        }, teacher.Id);

        Assert.Equal(SubmissionStatus.Graded, graded.Status);
        Assert.Equal(95, graded.Grade);
        Assert.Equal("Great work", graded.Feedback);
    }

    [Fact]
    public async Task StudentCannotEditAfterGraded()
    {
        var teacher = CreateTeacher("teacher@unic.test");
        var student = CreateStudent("student@unic.test");
        var (context, taskService, submissionService) = CreateServices();

        context.Users.AddRange(teacher, student);
        var subject = CreateSubject(teacher);
        context.Subjects.Add(subject);

        var community = CreateSubjectCommunity(subject.Id);
        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = student.Id,
            Role = CommunityRole.Member,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var task = await taskService.CreateTaskAsync(new CreateTaskDto
        {
            Title = "Assignment 1",
            Description = "Solve problems",
            DueDate = DateTime.UtcNow.AddDays(7),
            MaxScore = 100,
            SubjectId = subject.Id
        }, teacher.Id);

        var submission = await submissionService.CreateSubmissionAsync(new CreateSubmissionDto
        {
            TaskId = task.Id,
            Content = "Attempt"
        }, student.Id);

        await submissionService.GradeSubmissionAsync(submission.Id, new GradeSubmissionDto
        {
            Grade = 88,
            Feedback = "Good"
        }, teacher.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => submissionService.UpdateSubmissionAsync(submission.Id, new UpdateSubmissionDto
        {
            Content = "Updated after grade"
        }, student.Id));
    }
}
