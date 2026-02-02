using MTU.Models.DTOs;

namespace MTU.Services
{
    public interface IPostService
    {
        Task<List<PostDto>> GetFeedAsync(int pageNumber, int pageSize, int? currentUserId = null);
        Task<List<PostDto>> GetUserPostsAsync(int userId, int? currentUserId = null);
        Task<PostDto> CreatePostAsync(CreatePostDto dto, int currentUserId);
        Task<int> GetTotalPagesAsync(int pageSize);
        Task<bool> DeletePostAsync(int postId, int currentUserId);
        Task<List<PostDto>> GetFriendFeedAsync(int pageNumber, int pageSize, int currentUserId);
        Task<List<PostDto>> GetExploreFeedAsync(int pageNumber, int pageSize, int currentUserId);
        Task<List<PostDto>> SearchPostsAsync(string query, int currentUserId, int limit = 20);
        Task<PostDto?> GetPostByIdAsync(int postId, int currentUserId);
        Task<bool> UpdatePrivacyAsync(int postId, string privacy, int currentUserId);
        Task<List<PostDto>> GetSavedPostsAsync(int pageNumber, int pageSize, int currentUserId);
    }
}

