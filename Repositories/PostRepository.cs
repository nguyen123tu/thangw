using Microsoft.EntityFrameworkCore;
using MTU.Data;
using MTU.Models.Entities;

namespace MTU.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly MTUSocialDbContext _context;

        public PostRepository(MTUSocialDbContext context)
        {
            _context = context;
        }

        public async Task<List<Post>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetByUserIdAsync(int userId)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Post?> GetByIdAsync(int postId)
        {
            return await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PostId == postId && !p.IsDeleted);
        }

        public async Task<Post> CreateAsync(Post post)
        {
            post.CreatedAt = DateTime.Now;
            post.UpdatedAt = DateTime.Now;
            post.IsDeleted = false;

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return post;
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Posts
                .Where(p => !p.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetLikeCountAsync(int postId)
        {
            return await _context.Likes
                .Where(l => l.PostId == postId)
                .CountAsync();
        }

        public async Task<int> GetCommentCountAsync(int postId)
        {
            return await _context.Comments
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .CountAsync();
        }

        public async Task DeleteAsync(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {                post.IsDeleted = true;
                post.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
    }
}

