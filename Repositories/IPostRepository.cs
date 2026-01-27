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
    }
}

