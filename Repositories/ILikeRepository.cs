using MTU.Models.Entities;

namespace MTU.Repositories
{
    public interface ILikeRepository
    {
        Task<Like?> GetAsync(int postId, int userId);
        Task<Like> CreateAsync(Like like);
        Task DeleteAsync(Like like);
        Task<bool> ExistsAsync(int postId, int userId);
    }
}

