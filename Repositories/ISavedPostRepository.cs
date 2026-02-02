using MTU.Models.Entities;

namespace MTU.Repositories
{
    public interface ISavedPostRepository
    {
        Task<bool> HasSavedAsync(int userId, int postId);
        Task SaveAsync(SavedPost savedPost);
        Task UnsaveAsync(int userId, int postId);
        Task<List<int>> GetSavedPostIdsAsync(int userId);
        Task<List<Post>> GetSavedPostsAsync(int userId, int pageNumber, int pageSize);
    }
}
