using Microsoft.EntityFrameworkCore;
using MTU.Data;
using MTU.Models.Entities;

namespace MTU.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly MTUSocialDbContext _context;

        public CommentRepository(MTUSocialDbContext context)
        {
            _context = context;
        }

        public async Task<List<Comment>> GetByPostIdAsync(int postId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment> CreateAsync(Comment comment)
        {
            comment.CreatedAt = DateTime.Now;
            comment.UpdatedAt = DateTime.Now;
            comment.IsDeleted = false;

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return comment;
        }
    }
}

