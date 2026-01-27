using MTU.Models.Entities;

namespace MTU.Repositories
{
    public interface ICommentRepository
    {
        Task<List<Comment>> GetByPostIdAsync(int postId);
        Task<Comment> CreateAsync(Comment comment);
    }
}

