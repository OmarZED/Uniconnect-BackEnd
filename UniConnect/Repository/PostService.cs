using Microsoft.EntityFrameworkCore;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Maping;
using UniConnect.Models;

namespace UniConnect.Repository
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostService> _logger;

        public PostService(ApplicationDbContext context, ILogger<PostService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PostDto> CreatePostAsync(CreatePostDto createPostDto, string authorId)
        {
            try
            {
                var author = await _context.Users.FirstOrDefaultAsync(u => u.Id == authorId && u.IsActive);
                if (author == null)
                {
                    throw new InvalidOperationException("Author not found.");
                }

                if (!string.IsNullOrEmpty(createPostDto.CommunityId))
                {
                    var community = await _context.Communities
                        .FirstOrDefaultAsync(c => c.Id == createPostDto.CommunityId && c.IsActive);

                    if (community == null)
                    {
                        throw new InvalidOperationException("Community not found.");
                    }

                    var isMember = await _context.CommunityMembers
                        .AnyAsync(cm => cm.CommunityId == createPostDto.CommunityId && cm.UserId == authorId && cm.IsActive);

                    if (!isMember)
                    {
                        throw new InvalidOperationException("User must be a community member to create a post.");
                    }
                }

                var post = new Post
                {
                    Title = createPostDto.Title,
                    Content = createPostDto.Content,
                    CommunityId = createPostDto.CommunityId,
                    AuthorId = authorId,
                    Visibility = createPostDto.Visibility,
                    ImageUrl = createPostDto.ImageUrl,
                    FileUrl = createPostDto.FileUrl,
                    FileName = createPostDto.FileName,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                await _context.Entry(post).Reference(p => p.Author).LoadAsync();
                if (!string.IsNullOrEmpty(post.CommunityId))
                {
                    await _context.Entry(post).Reference(p => p.Community).LoadAsync();
                }

                return MapToPostDto(post, authorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                throw;
            }
        }

        public async Task<List<PostDto>> GetCommunityPostsAsync(string communityId, string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var community = await _context.Communities
                    .FirstOrDefaultAsync(c => c.Id == communityId && c.IsActive);

                if (community == null)
                {
                    throw new KeyNotFoundException("Community not found.");
                }

                var isMember = await _context.CommunityMembers
                    .AnyAsync(cm => cm.CommunityId == communityId && cm.UserId == userId && cm.IsActive);

                if (!isMember)
                {
                    throw new UnauthorizedAccessException("User must be a community member to view posts.");
                }

                var posts = await _context.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Reactions)
                    .Include(p => p.Comments)
                    .Where(p => p.CommunityId == communityId && p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return posts.Select(p => MapToPostDto(p, userId)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community posts for community: {CommunityId}", communityId);
                throw;
            }
        }

        public async Task<PostDto> GetPostByIdAsync(string id, string? userId = null)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Community)
                    .Include(p => p.Reactions)
                    .Include(p => p.Comments)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (post == null)
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(post.CommunityId) && !string.IsNullOrEmpty(userId))
                {
                    var isMember = await _context.CommunityMembers
                        .AnyAsync(cm => cm.CommunityId == post.CommunityId && cm.UserId == userId && cm.IsActive);

                    if (!isMember)
                    {
                        throw new UnauthorizedAccessException("User must be a community member to view this post.");
                    }
                }

                return MapToPostDto(post, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post by ID: {PostId}", id);
                throw;
            }
        }

        public async Task<PostDto> UpdatePostAsync(string id, UpdatePostDto updatePostDto, string userId)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Community)
                    .Include(p => p.Reactions)
                    .Include(p => p.Comments)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (post == null)
                {
                    throw new KeyNotFoundException("Post not found.");
                }

                if (post.AuthorId != userId)
                {
                    throw new UnauthorizedAccessException("Only the author can edit this post.");
                }

                if (!string.IsNullOrEmpty(updatePostDto.Title))
                {
                    post.Title = updatePostDto.Title;
                }

                if (!string.IsNullOrEmpty(updatePostDto.Content))
                {
                    post.Content = updatePostDto.Content;
                }

                if (updatePostDto.Visibility.HasValue)
                {
                    post.Visibility = updatePostDto.Visibility.Value;
                }

                if (updatePostDto.ImageUrl != null)
                {
                    post.ImageUrl = updatePostDto.ImageUrl;
                }

                if (updatePostDto.FileUrl != null)
                {
                    post.FileUrl = updatePostDto.FileUrl;
                }

                if (updatePostDto.FileName != null)
                {
                    post.FileName = updatePostDto.FileName;
                }

                post.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return MapToPostDto(post, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post: {PostId}", id);
                throw;
            }
        }

        public async Task<bool> DeletePostAsync(string id, string userId)
        {
            try
            {
                var post = await _context.Posts
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (post == null)
                {
                    throw new KeyNotFoundException("Post not found.");
                }

                if (post.AuthorId != userId)
                {
                    throw new UnauthorizedAccessException("Only the author can delete this post.");
                }

                post.IsActive = false;
                post.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post: {PostId}", id);
                throw;
            }
        }

        public async Task<List<PostDto>> GetUserFeedAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var communityIds = await _context.CommunityMembers
                    .Where(cm => cm.UserId == userId && cm.IsActive)
                    .Select(cm => cm.CommunityId)
                    .ToListAsync();

                var posts = await _context.Posts
                    .Include(p => p.Author)
                    .Include(p => p.Community)
                    .Include(p => p.Reactions)
                    .Include(p => p.Comments)
                    .Where(p => p.IsActive && (
                        (p.CommunityId != null && communityIds.Contains(p.CommunityId)) ||
                        p.AuthorId == userId))
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return posts.Select(p => MapToPostDto(p, userId)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user feed for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> AddPostReactionAsync(string postId, string userId, ReactionType reactionType)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.Reactions)
                    .FirstOrDefaultAsync(p => p.Id == postId && p.IsActive);

                if (post == null)
                {
                    throw new KeyNotFoundException("Post not found.");
                }

                if (!string.IsNullOrEmpty(post.CommunityId))
                {
                    var isMember = await _context.CommunityMembers
                        .AnyAsync(cm => cm.CommunityId == post.CommunityId && cm.UserId == userId && cm.IsActive);

                    if (!isMember)
                    {
                        throw new UnauthorizedAccessException("User must be a community member to react.");
                    }
                }

                var existing = post.Reactions.FirstOrDefault(r => r.UserId == userId);
                if (existing != null)
                {
                    existing.ReactionType = reactionType;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var reaction = new PostReaction
                    {
                        PostId = postId,
                        UserId = userId,
                        ReactionType = reactionType,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.PostReactions.Add(reaction);
                }

                post.TotalReactions = await _context.PostReactions.CountAsync(r => r.PostId == postId);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reaction to post: {PostId}", postId);
                throw;
            }
        }

        public async Task<bool> RemovePostReactionAsync(string postId, string userId)
        {
            try
            {
                var reaction = await _context.PostReactions
                    .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);

                if (reaction == null)
                {
                    throw new InvalidOperationException("Reaction not found.");
                }

                _context.PostReactions.Remove(reaction);
                await _context.SaveChangesAsync();

                var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
                if (post != null)
                {
                    post.TotalReactions = await _context.PostReactions.CountAsync(r => r.PostId == postId);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing reaction from post: {PostId}", postId);
                throw;
            }
        }

        public async Task<List<PostReactionDto>> GetPostReactionsAsync(string postId)
        {
            try
            {
                var reactions = await _context.PostReactions
                    .Include(r => r.User)
                    .Where(r => r.PostId == postId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return reactions.Select(r => new PostReactionDto
                {
                    Id = r.Id,
                    PostId = r.PostId,
                    UserId = r.UserId,
                    UserName = $"{r.User.FirstName} {r.User.LastName}",
                    ReactionType = r.ReactionType,
                    CreatedAt = r.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reactions for post: {PostId}", postId);
                throw;
            }
        }

        private PostDto MapToPostDto(Post post, string? currentUserId)
        {
            var currentReaction = post.Reactions.FirstOrDefault(r => r.UserId == currentUserId)?.ReactionType;

            return new PostDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                CommunityId = post.CommunityId,
                CommunityName = post.Community?.Name,
                AuthorId = post.AuthorId,
                AuthorName = $"{post.Author.FirstName} {post.Author.LastName}",
                AuthorProfilePicture = post.Author.ProfilePictureUrl,
                Visibility = post.Visibility,
                ImageUrl = post.ImageUrl,
                FileUrl = post.FileUrl,
                FileName = post.FileName,
                LikeCount = post.LikeCount,
                LoveCount = post.LoveCount,
                WowCount = post.WowCount,
                LaughCount = post.LaughCount,
                SadCount = post.SadCount,
                AngryCount = post.AngryCount,
                TotalReactions = post.TotalReactions,
                CommentCount = post.CommentCount,
                ShareCount = post.ShareCount,
                CurrentUserReaction = currentReaction,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                IsActive = post.IsActive,
                CanEdit = currentUserId != null && post.AuthorId == currentUserId,
                CanDelete = currentUserId != null && post.AuthorId == currentUserId
            };
        }
    }
}
