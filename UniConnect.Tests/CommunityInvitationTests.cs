using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using UniConnect.Dtos;
using UniConnect.Maping;
using UniConnect.Models;
using UniConnect.Repository;

namespace UniConnect.Tests;

public class CommunityInvitationTests
{
    private static (ApplicationDbContext context, CommunityService service, ApplicationUser currentUser) CreateServiceWithUser(ApplicationUser user)
    {
        var dbName = $"CommunityInvitationTests_{Guid.NewGuid()}";
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

        return (context, service, user);
    }

    [Fact]
    public async Task CreateInvitation_AllowsCommunityAdmin()
    {
        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin@unic.test",
            UserName = "admin@unic.test",
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.DepartmentManager,
            IsActive = true
        };

        var (context, service, currentUser) = CreateServiceWithUser(admin);

        var community = new Community
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Dept Community",
            Description = "Department",
            Type = CommunityType.Department,
            AllowPosts = true,
            AutoJoin = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = currentUser.Id,
            Role = CommunityRole.Admin,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var invitation = await service.CreateInvitationAsync(community.Id, currentUser.Id, "student@unic.test");

        Assert.Equal(community.Id, invitation.CommunityId);
        Assert.Equal("student@unic.test", invitation.InviteeEmail);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }

    [Fact]
    public async Task CreateInvitation_BlocksNonAdmin()
    {
        var member = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "member@unic.test",
            UserName = "member@unic.test",
            FirstName = "Member",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var (context, service, currentUser) = CreateServiceWithUser(member);

        var community = new Community
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Dept Community",
            Description = "Department",
            Type = CommunityType.Department,
            AllowPosts = true,
            AutoJoin = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = currentUser.Id,
            Role = CommunityRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateInvitationAsync(community.Id, currentUser.Id, "student@unic.test"));
    }

    [Fact]
    public async Task AcceptInvitation_JoinsCommunity()
    {
        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin2@unic.test",
            UserName = "admin2@unic.test",
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.DepartmentManager,
            IsActive = true
        };

        var invitee = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "invitee@unic.test",
            UserName = "invitee@unic.test",
            FirstName = "Invitee",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var (context, service, currentUser) = CreateServiceWithUser(admin);
        context.Users.Add(invitee);

        var community = new Community
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Dept Community",
            Description = "Department",
            Type = CommunityType.Department,
            AllowPosts = true,
            AutoJoin = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = currentUser.Id,
            Role = CommunityRole.Admin,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var invitation = await service.CreateInvitationAsync(community.Id, currentUser.Id, invitee.Email!);

        var accepted = await service.RespondToInvitationAsync(invitation.Id, invitee.Email!, true);

        Assert.Equal(InvitationStatus.Accepted, accepted.Status);
        Assert.True(context.CommunityMembers.Any(m => m.CommunityId == community.Id && m.UserId == invitee.Id && m.IsActive));
    }
}
