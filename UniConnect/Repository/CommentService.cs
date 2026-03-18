using Microsoft.EntityFrameworkCore;
using UniConnect.Dtos;
using UniConnect.INterfface;
using UniConnect.Maping;
using UniConnect.Models;

namespace UniConnect.Repository
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommentService> _logger;

        public CommentService(ApplicationDbContext context, ILogger<CommentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CommentDto> CreateCommentAsync(CreateCommentDto createCommentDto, string authorId)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.Community)
                    .FirstOrDefaultAsync(p => p.Id == createCommentDto.PostId && p.IsActive);

                if (post == null)
                {
                    throw new KeyNotFoundException("Post not found.");
                }

                if (!string.IsNullOrEmpty(post.CommunityId))
                {
                    var isMember = await _context.CommunityMembers
                        .AnyAsync(cm => cm.CommunityId == post.CommunityId && cm.UserId == authorId && cm.IsActive);

                    if (!isMember)
                    {
                        throw new UnauthorizedAccessException("User must be a community member to comment.");
                    }
                }

                if (!string.IsNullOrEmpty(createCommentDto.ParentCommentId))
                {
                    var parent = await _context.Comments
                        .FirstOrDefaultAsync(c => c.Id == createCommentDto.ParentCommentId && c.IsActive);

                    if (parent == null || parent.PostId != createCommentDto.PostId)
                    {
                        throw new InvalidOperationException("Parent comment not found for this post.");
                    }
                }

                var comment = new Comment
                {
                    Content = createCommentDto.Content,
                    PostId = createCommentDto.PostId,
                    AuthorId = authorId,
                    ParentCommentId = createCommentDto.ParentCommentId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                post.CommentCount = await _context.Comments.CountAsync(c => c.PostId == post.Id && c.IsActive);
                await _context.SaveChangesAsync();

                await _context.Entry(comment).Reference(c => c.Author).LoadAsync();
                return MapToCommentDto(comment, authorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                throw;
            }
        }

        public async Task<List<CommentDto>> GetPostCommentsAsync(string postId, string? userId = null)
        {
            try
            {
                var comments = await _context.Comments
                    .Include(c => c.Author)
                    .Include(c => c.Votes)
                    .Where(c => c.PostId == postId && c.IsActive)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                var lookup = comments.ToDictionary(c => c.Id, c => MapToCommentDto(c, userId));

                foreach (var c in comments)
                {
                    if (!string.IsNullOrEmpty(c.ParentCommentId) && lookup.ContainsKey(c.ParentCommentId))
                    {
                        lookup[c.ParentCommentId].Replies.Add(lookup[c.Id]);
                    }
                }

                return lookup.Values.Where(d => string.IsNullOrEmpty(d.ParentCommentId)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for post: {PostId}", postId);
                throw;
            }
        }

        public async Task<CommentDto> GetCommentByIdAsync(string id, string? userId = null)
        {
            try
            {
                var comment = await _context.Comments
                    .Include(c => c.Author)
                    .Include(c => c.Votes)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (comment == null) return null;

                return MapToCommentDto(comment, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment by ID: {CommentId}", id);
                throw;
            }
        }

        public async Task<CommentDto> UpdateCommentAsync(string id, UpdateCommentDto updateCommentDto, string userId)
        {
            try
            {
                var comment = await _context.Comments
                    .Include(c => c.Author)
                    .Include(c => c.Votes)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (comment == null)
                {
                    throw new KeyNotFoundException("Comment not found.");
                }

                if (comment.AuthorId != userId)
                {
                    throw new UnauthorizedAccessException("Only the author can edit this comment.");
                }

                comment.Content = updateCommentDto.Content;
                comment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return MapToCommentDto(comment, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment: {CommentId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCommentAsync(string id, string userId)
        {
            try
            {
                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (comment == null)
                {
                    throw new KeyNotFoundException("Comment not found.");
                }

                if (comment.AuthorId != userId)
                {
                    throw new UnauthorizedAccessException("Only the author can delete this comment.");
                }

                comment.IsActive = false;
                comment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == comment.PostId);
                if (post != null)
                {
                    post.CommentCount = await _context.Comments.CountAsync(c => c.PostId == post.Id && c.IsActive);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment: {CommentId}", id);
                throw;
            }
        }

        public async Task<bool> VoteOnCommentAsync(string commentId, string userId, VoteType voteType)
        {
            try
            {
                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == commentId && c.IsActive);

                if (comment == null)
                {
                    throw new KeyNotFoundException("Comment not found.");
                }

                var existingVote = await _context.CommentVotes
                    .FirstOrDefaultAsync(v => v.CommentId == commentId && v.UserId == userId);

                if (existingVote != null)
                {
                    if (existingVote.VoteType == voteType)
                    {
                        return true;
                    }

                    if (existingVote.VoteType == VoteType.Upvote) comment.UpvoteCount--;
                    if (existingVote.VoteType == VoteType.Downvote) comment.DownvoteCount--;

                    existingVote.VoteType = voteType;
                    existingVote.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var vote = new CommentVote
                    {
                        CommentId = commentId,
                        UserId = userId,
                        VoteType = voteType,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CommentVotes.Add(vote);
                }

                if (voteType == VoteType.Upvote) comment.UpvoteCount++;
                if (voteType == VoteType.Downvote) comment.DownvoteCount++;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voting on comment: {CommentId}", commentId);
                throw;
            }
        }

        public async Task<bool> RemoveVoteFromCommentAsync(string commentId, string userId)
        {
            try
            {
                var existingVote = await _context.CommentVotes
                    .FirstOrDefaultAsync(v => v.CommentId == commentId && v.UserId == userId);

                if (existingVote == null)
                {
                    throw new InvalidOperationException("Vote not found.");
                }

                var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
                if (comment != null)
                {
                    if (existingVote.VoteType == VoteType.Upvote) comment.UpvoteCount--;
                    if (existingVote.VoteType == VoteType.Downvote) comment.DownvoteCount--;
                }

                _context.CommentVotes.Remove(existingVote);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing vote from comment: {CommentId}", commentId);
                throw;
            }
        }

        public async Task<CommentVoteDto> GetUserVoteOnCommentAsync(string commentId, string userId)
        {
            try
            {
                var vote = await _context.CommentVotes
                    .Include(v => v.User)
                    .FirstOrDefaultAsync(v => v.CommentId == commentId && v.UserId == userId);

                if (vote == null) return null;

                return new CommentVoteDto
                {
                    Id = vote.Id,
                    CommentId = vote.CommentId,
                    UserId = vote.UserId,
                    UserName = $"{vote.User.FirstName} {vote.User.LastName}",
                    VoteType = vote.VoteType,
                    CreatedAt = vote.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user vote for comment: {CommentId}", commentId);
                throw;
            }
        }

        public async Task<List<CommentDto>> GetCommentRepliesAsync(string commentId, string? userId = null)
        {
            try
            {
                var replies = await _context.Comments
                    .Include(c => c.Author)
                    .Include(c => c.Votes)
                    .Where(c => c.ParentCommentId == commentId && c.IsActive)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                return replies.Select(r => MapToCommentDto(r, userId)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment replies for comment: {CommentId}", commentId);
                throw;
            }
        }

        private CommentDto MapToCommentDto(Comment comment, string? currentUserId)
        {
            var currentVote = comment.Votes.FirstOrDefault(v => v.UserId == currentUserId)?.VoteType;

            return new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                PostId = comment.PostId,
                AuthorId = comment.AuthorId,
                AuthorName = $"{comment.Author.FirstName} {comment.Author.LastName}",
                AuthorProfilePicture = comment.Author.ProfilePictureUrl,
                ParentCommentId = comment.ParentCommentId,
                UpvoteCount = comment.UpvoteCount,
                DownvoteCount = comment.DownvoteCount,
                Score = comment.Score,
                CurrentUserVote = currentVote,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsActive = comment.IsActive,
                CanEdit = currentUserId != null && comment.AuthorId == currentUserId,
                CanDelete = currentUserId != null && comment.AuthorId == currentUserId
            };
        }
    }
}
