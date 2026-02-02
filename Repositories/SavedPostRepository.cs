using Microsoft.EntityFrameworkCore;
using MTU.Data;
using MTU.Models.Entities;

namespace MTU.Repositories
{
    public class SavedPostRepository : ISavedPostRepository
    {
        private readonly MTUSocialDbContext _context;

        public SavedPostRepository(MTUSocialDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasSavedAsync(int userId, int postId)
        {
            return await _context.SavedPosts
                .AnyAsync(s => s.UserId == userId && s.PostId == postId);
        }

        public async Task SaveAsync(SavedPost savedPost)
        {
            if (!await HasSavedAsync(savedPost.UserId, savedPost.PostId))
            {
                _context.SavedPosts.Add(savedPost);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UnsaveAsync(int userId, int postId)
        {
            var savedPost = await _context.SavedPosts
                .FirstOrDefaultAsync(s => s.UserId == userId && s.PostId == postId);
            
            if (savedPost != null)
            {
                _context.SavedPosts.Remove(savedPost);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<int>> GetSavedPostIdsAsync(int userId)
        {
            return await _context.SavedPosts
                .Where(s => s.UserId == userId)
                .Select(s => s.PostId)
                .ToListAsync();
        }

        public async Task<List<Post>> GetSavedPostsAsync(int userId, int pageNumber, int pageSize)
        {
            return await _context.SavedPosts
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SavedAt)
                .Select(s => s.Post)
                .Include(p => p.User)
                .Where(p => !p.IsDeleted) // Ensure post is not deleted even if saved record exists
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
