using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UniConnect.Dtos;
using UniConnect.Maping;
using UniConnect.Models;
using UniConnect.Repository;

namespace UniConnect.Tests;

public class PostAndCommentServiceTests
{
    private static ApplicationDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static (PostService postService, CommentService commentService, ApplicationDbContext context) CreateServices()
    {
        var context = CreateDbContext($"PostAndCommentServiceTests_{Guid.NewGuid()}");
        var postService = new PostService(context, NullLogger<PostService>.Instance);
        var commentService = new CommentService(context, NullLogger<CommentService>.Instance);
        return (postService, commentService, context);
    }

    [Fact]
    public async Task CreatePost_AndReact_AndCommentFlow()
    {
        var (postService, commentService, context) = CreateServices();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user@unic.test",
            UserName = "user@unic.test",
            FirstName = "User",
            LastName = "One",
            Role = UserRole.Student,
            IsActive = true
        };

        var community = new Community
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Community",
            Description = "Community",
            Type = CommunityType.Department,
            AllowPosts = true,
            AutoJoin = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Users.Add(user);
        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = user.Id,
            Role = CommunityRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var post = await postService.CreatePostAsync(new CreatePostDto
        {
            Title = "Hello",
            Content = "First post",
            CommunityId = community.Id
        }, user.Id);

        Assert.Equal("Hello", post.Title);

        var reacted = await postService.AddPostReactionAsync(post.Id, user.Id, ReactionType.Like);
        Assert.True(reacted);

        var comment = await commentService.CreateCommentAsync(new CreateCommentDto
        {
            Content = "Nice post",
            PostId = post.Id
        }, user.Id);

        Assert.Equal(post.Id, comment.PostId);
        Assert.Equal("Nice post", comment.Content);
    }

    [Fact]
    public async Task UpdateAndDeletePost_OnlyAuthor()
    {
        var (postService, _, context) = CreateServices();

        var author = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "author@unic.test",
            UserName = "author@unic.test",
            FirstName = "Author",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var other = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "other@unic.test",
            UserName = "other@unic.test",
            FirstName = "Other",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var community = new Community
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Community",
            Description = "Community",
            Type = CommunityType.Department,
            AllowPosts = true,
            AutoJoin = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Users.AddRange(author, other);
        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = author.Id,
            Role = CommunityRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var post = await postService.CreatePostAsync(new CreatePostDto
        {
            Title = "Hello",
            Content = "First post",
            CommunityId = community.Id
        }, author.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            postService.UpdatePostAsync(post.Id, new UpdatePostDto { Title = "Hack" }, other.Id));

        var updated = await postService.UpdatePostAsync(post.Id, new UpdatePostDto { Title = "Updated" }, author.Id);
        Assert.Equal("Updated", updated.Title);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            postService.DeletePostAsync(post.Id, other.Id));

        var deleted = await postService.DeletePostAsync(post.Id, author.Id);
        Assert.True(deleted);
    }

    [Fact]
    public async Task UpdateAndDeleteComment_OnlyAuthor()
    {
        var (postService, commentService, context) = CreateServices();

        var author = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "author2@unic.test",
            UserName = "author2@unic.test",
            FirstName = "Author",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var other = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "other2@unic.test",
            UserName = "other2@unic.test",
            FirstName = "Other",
            LastName = "User",
            Role = UserRole.Student,
            IsActive = true
        };

        var community = new Community
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Community",
            Description = "Community",
            Type = CommunityType.Department,
            AllowPosts = true,
            AutoJoin = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Users.AddRange(author, other);
        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = author.Id,
            Role = CommunityRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var post = await postService.CreatePostAsync(new CreatePostDto
        {
            Title = "Hello",
            Content = "First post",
            CommunityId = community.Id
        }, author.Id);

        var comment = await commentService.CreateCommentAsync(new CreateCommentDto
        {
            Content = "Nice post",
            PostId = post.Id
        }, author.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            commentService.UpdateCommentAsync(comment.Id, new UpdateCommentDto { Content = "Hack" }, other.Id));

        var updated = await commentService.UpdateCommentAsync(comment.Id, new UpdateCommentDto { Content = "Updated" }, author.Id);
        Assert.Equal("Updated", updated.Content);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            commentService.DeleteCommentAsync(comment.Id, other.Id));

        var deleted = await commentService.DeleteCommentAsync(comment.Id, author.Id);
        Assert.True(deleted);
    }

    [Fact]
    public async Task RepliesAndVotes_Work()
    {
        var (postService, commentService, context) = CreateServices();

        var userA = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "usera@unic.test",
            UserName = "usera@unic.test",
            FirstName = "User",
            LastName = "A",
            Role = UserRole.Student,
            IsActive = true
        };

        var userB = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "userb@unic.test",
            UserName = "userb@unic.test",
            FirstName = "User",
            LastName = "B",
            Role = UserRole.Student,
            IsActive = true
        };

        var community = new Community
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Community",
            Description = "Community",
            Type = CommunityType.Department,
            AllowPosts = true,
            AutoJoin = false,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Users.AddRange(userA, userB);
        context.Communities.Add(community);
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = userA.Id,
            Role = CommunityRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });
        context.CommunityMembers.Add(new CommunityMember
        {
            CommunityId = community.Id,
            UserId = userB.Id,
            Role = CommunityRole.Member,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var post = await postService.CreatePostAsync(new CreatePostDto
        {
            Title = "Hello",
            Content = "First post",
            CommunityId = community.Id
        }, userA.Id);

        var root = await commentService.CreateCommentAsync(new CreateCommentDto
        {
            Content = "Root comment",
            PostId = post.Id
        }, userA.Id);

        var reply = await commentService.CreateCommentAsync(new CreateCommentDto
        {
            Content = "Reply comment",
            PostId = post.Id,
            ParentCommentId = root.Id
        }, userB.Id);

        var replies = await commentService.GetCommentRepliesAsync(root.Id, userA.Id);
        Assert.Single(replies);
        Assert.Equal(reply.Id, replies[0].Id);

        await commentService.VoteOnCommentAsync(root.Id, userB.Id, VoteType.Upvote);
        var rootUpdated = await commentService.GetCommentByIdAsync(root.Id, userB.Id);
        Assert.Equal(1, rootUpdated.UpvoteCount);
        Assert.Equal(1, rootUpdated.Score);

        await commentService.VoteOnCommentAsync(root.Id, userB.Id, VoteType.Downvote);
        rootUpdated = await commentService.GetCommentByIdAsync(root.Id, userB.Id);
        Assert.Equal(0, rootUpdated.UpvoteCount);
        Assert.Equal(1, rootUpdated.DownvoteCount);
        Assert.Equal(-1, rootUpdated.Score);
    }
}
