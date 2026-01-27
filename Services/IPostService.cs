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
    }
}

