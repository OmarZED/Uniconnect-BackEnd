using UniConnect.Dtos;
using UniConnect.Models;

namespace UniConnect.INterfface
{
    public interface ICommentService
    {
        // Comment CRUD
        Task<CommentDto> CreateCommentAsync(CreateCommentDto createCommentDto, string authorId);
        Task<List<CommentDto>> GetPostCommentsAsync(string postId, string? userId = null);
        Task<CommentDto> GetCommentByIdAsync(string id, string? userId = null);
        Task<CommentDto> UpdateCommentAsync(string id, UpdateCommentDto updateCommentDto, string userId);
        Task<bool> DeleteCommentAsync(string id, string userId);

        // Comment voting
        Task<bool> VoteOnCommentAsync(string commentId, string userId, VoteType voteType);
        Task<bool> RemoveVoteFromCommentAsync(string commentId, string userId);
        Task<CommentVoteDto> GetUserVoteOnCommentAsync(string commentId, string userId);

        // Nested comments
        Task<List<CommentDto>> GetCommentRepliesAsync(string commentId, string? userId = null);
    }
}
