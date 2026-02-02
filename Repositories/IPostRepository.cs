using MTU.Models.Entities;

namespace MTU.Repositories
{
    public interface IPostRepository
    {
        Task<List<Post>> GetAllAsync(int pageNumber, int pageSize);
        Task<List<Post>> GetByUserIdAsync(int userId);
        Task<Post?> GetByIdAsync(int postId);
        Task<Post> CreateAsync(Post post);
        Task<int> GetTotalCountAsync();
        Task<int> GetLikeCountAsync(int postId);
        Task<int> GetCommentCountAsync(int postId);
        Task DeleteAsync(int postId);
        Task<List<Post>> GetFriendPostsAsync(int userId, int pageNumber, int pageSize);
        Task<List<Post>> GetExplorePostsAsync(int userId, int pageNumber, int pageSize);
        Task<List<Post>> SearchAsync(string query, int limit);
        Task UpdatePrivacyAsync(int postId, string privacy);
    }
}

