using UniConnect.Dtos;
using UniConnect.Models;

namespace UniConnect.INterfface
{
    public interface IPostService
    {
        // Post CRUD
        Task<PostDto> CreatePostAsync(CreatePostDto createPostDto, string authorId);
        Task<List<PostDto>> GetCommunityPostsAsync(string communityId, int page = 1, int pageSize = 20);
        Task<PostDto> GetPostByIdAsync(string id);
        Task<bool> DeletePostAsync(string id, string userId);

        // User feed
        Task<List<PostDto>> GetUserFeedAsync(string userId, int page = 1, int pageSize = 20);

        // Post reactions
        Task<bool> AddPostReactionAsync(string postId, string userId, ReactionType reactionType);
        Task<bool> RemovePostReactionAsync(string postId, string userId);
        Task<List<PostReactionDto>> GetPostReactionsAsync(string postId);
    }
}
